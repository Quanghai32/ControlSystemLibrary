Imports System.IO.Ports
Imports System.Windows.Forms

Public Class XBee
	Friend ChildComponent As List(Of XbeeDevices)
	Const MAX_DATA_LENGTH As Byte = 8
	Const MAX_BUFFER_SIZE As Byte = 30
	Const TIME_TRANSMIT As Integer = 10
	Enum RFID_command
		TRANSMIT = &H10
		RECEIVE = &H90
		RESPOND = &H88
	End Enum

	Enum AT_COMMAND_ENUM
		SERIAL_NUMBER_LOW = &H534C 'SL
		PAN_ID = &H4944	'ID
	End Enum
	Public Structure XBeeDataStruct
		Public ID As UInt32
		Public len As Byte
		Public data() As Byte
		Public Sub New(ByVal Num As Byte)
			Dim i As Byte
			data = New Byte(MAX_DATA_LENGTH - 1) {}
			For i = 0 To MAX_DATA_LENGTH - 1
				data(i) = Num
			Next
		End Sub
	End Structure
	''' <summary>
	''' Personal area network (PAN) ID
	''' </summary>
	Public Property PID As Int16
	''' <summary>
	''' Physical address of Xbee
	''' </summary>
	Public Property Address As UInt32

	Private port As SerialPort
	Private transmitData() As Byte

	Private Receive() As Byte
	Private ReceiveCount As Byte = 0
	Private ReceiveStep As Byte = 0
	Private ReceiveLenArray() As Byte
	Private ReceiveLen As UInt16
	Private ReceiveCheck As Byte

	Private WithEvents TransmitCounter As System.Timers.Timer
	Private WithEvents TimerReceiveCheck As System.Timers.Timer
	Private DataReceiveBuffer(MAX_BUFFER_SIZE - 1) As XBeeDataStruct
	Private DataReceiveBufferReadIndex As Byte = 0
	Private DataReceiveBufferWriteIndex As Byte = 0
	Private DataTransmitBuffer(MAX_BUFFER_SIZE - 1) As XBeeDataStruct
	Private DataTransmitBufferReadIndex As Byte = 0
	Private DataTransmitBufferWriteIndex As Byte = 0
	''' <summary>
	''' Creates and returns a new XBee object using the specified Port name, baudrate,Xbee address and Xbee PAN ID
	''' </summary>
	''' <param name="PortName">The port to use (for example, COM1).</param>
	''' <param name="PortBaudRate">The baud rate of COM port</param>
	''' <param name="XBeeAddress">Address of Xbee (32 bit)</param>
	''' <param name="XBeePID">PAN ID of Xbee</param>
	Public Sub New(ByVal PortName As String, ByVal PortBaudRate As Integer, ByVal XBeeAddress As UInt32, ByVal XBeePID As Byte)
		MyBase.New()
		SettingPort(PortName, PortBaudRate)
		Address = XBeeAddress
		PID = XBeePID
		init()
	End Sub
	''' <summary>
	''' Creates and returns a new XBee object using the specified Xbee address and Xbee PAN ID
	''' </summary>
	''' <param name="XBeeAddress">Address of Xbee (32 bit)</param>
	''' <param name="XBeePID">PAN ID of Xbee</param>
	Public Sub New(ByVal XBeeAddress As UInt32, ByVal XBeePID As Byte)
		MyBase.New()
		Address = XBeeAddress
		PID = XBeePID
		init()
	End Sub
	''' <summary>
	''' Creates and returns a new XBee object using the specified Port name, Xbee address and Xbee PAN ID
	''' </summary>
	''' <param name="PortName">The port to use (for example, COM1).</param>
	''' <param name="XBeeAddress">Address of Xbee (32 bit)</param>
	''' <param name="XBeePID">PAN ID of Xbee</param>
	''' <remarks>The default baudrate of COM port will be 9600</remarks>
	Public Sub New(ByVal PortName As String, ByVal XBeeAddress As UInt32, ByVal XBeePID As Byte)
		MyBase.New()
		SettingPort(PortName, 9600)
		Address = XBeeAddress
		PID = XBeePID
		init()
	End Sub
	''' <summary>
	''' Creates and returns a new XBee object
	''' </summary>
	''' <remarks></remarks>
	Public Sub New()
		MyBase.New()
		Address = 0
		PID = 0
		init()
	End Sub


	Private Sub init()
		Dim i As Byte
		setupTransmitData()
		Receive = New Byte(100) {}
		DataReceiveBuffer = New XBeeDataStruct(MAX_BUFFER_SIZE - 1) {}
		For i = 0 To MAX_BUFFER_SIZE - 1
			DataReceiveBuffer(i) = New XBeeDataStruct(0)
		Next
		ReceiveLenArray = New Byte(1) {}
		TransmitCounter = New System.Timers.Timer()
		TransmitCounter.Interval = TIME_TRANSMIT
		TransmitCounter.Enabled = False

		TimerReceiveCheck = New System.Timers.Timer()
		TimerReceiveCheck.Interval = 10
		TimerReceiveCheck.Enabled = False

		ChildComponent = New List(Of XbeeDevices)
	End Sub
	Private Sub setupTransmitData()
		transmitData = New Byte(29) {}
		transmitData(0) = &H7E		'Start Delimiter
		transmitData(1) = &H0		'High byte of length
		transmitData(2) = &H16		'Low byte of length
		transmitData(3) = &H10		'Frame tyte - Transmit command
		transmitData(4) = &H1	   'Frame ID
		'From byte 5 to byte 12 is 64-bits address
		transmitData(5) = &H0
		transmitData(6) = &H13
		transmitData(7) = &HA2
		transmitData(8) = &H0
		transmitData(9) = &H40
		transmitData(10) = &HA
		transmitData(11) = &H1
		transmitData(12) = &H27
		'Byte 13 and 14 is reserved
		transmitData(13) = &HFF
		transmitData(14) = &HFE
		transmitData(15) = &H0		'Broadcast Radius
		transmitData(16) = &H0		'Transmit optimons
		'Byte 17 to 24 is RF data that is sent to the destination device
		transmitData(17) = &H1
		transmitData(18) = &H2
		transmitData(19) = &H3
		transmitData(20) = &H4
		transmitData(21) = &H5
		transmitData(22) = &H6
		transmitData(23) = &H7
		transmitData(24) = &H8
		transmitData(25) = &H0		'Checksum = 0xFF - the 8bit sum of byte from of bytes from offset 3 to this byte
	End Sub
	Private Sub setChecksumByte()
		Dim i As Byte
		Dim sum As Integer
		Dim sumArray() As Byte
		Dim len As Byte
		len = transmitData(2)
		sum = 0
		For i = 3 To len + 2
			sum = sum + transmitData(i)
		Next
		sumArray = BitConverter.GetBytes(sum)
		transmitData(len + 3) = &HFF - sumArray(0)
	End Sub

    Private Sub SendData(ByVal Dest As UInt32, ByVal data() As Byte, ByVal len As Byte)
        If IsNothing(port) Then
            Return
        ElseIf Not port.IsOpen Then
            Return
        End If
        Dim i As Byte
        Dim DestByte As Byte
        'Set length
        transmitData(2) = 14 + len
        For i = 0 To 3
            DestByte = (Dest >> (24 - i * 8)) And &HFF
            transmitData(9 + i) = DestByte
        Next
        For i = 0 To len - 1
            transmitData(17 + i) = data(i)
        Next
        setChecksumByte()
        port.Write(transmitData, 0, 26)
    End Sub
	Private Sub Port_DataReceived(ByVal sender As System.Object, ByVal e As System.IO.Ports.SerialDataReceivedEventArgs)
		Dim DataReceive As Byte
        Dim i As Integer
        If port.IsOpen = False Then
            Return
        End If
        For i = 0 To port.BytesToRead - 1
            DataReceive = port.ReadByte()
            If (DataReceive = &H7E) And (ReceiveStep = 0) Then
                ReceiveStep = 1
                Exit Sub
            End If
            Select Case ReceiveStep
                Case 1
                    ReceiveLenArray(1) = DataReceive
                    ReceiveStep += 1
                Case 2
                    ReceiveLenArray(0) = DataReceive
                    ReceiveLen = BitConverter.ToInt16(ReceiveLenArray, 0)
                    ReceiveCount = 0
                    ReceiveStep += 1
                Case 3
                    Receive(ReceiveCount) = DataReceive
                    ReceiveCount += 1
                    If ReceiveCount >= ReceiveLen Then
                        ReceiveStep += 1
                    End If
                Case 4
                    ReceiveCheck = DataReceive
                    If ReceiveCheckSum() Then
                        analizeInformationReceived()
                    End If
                    ReceiveStep = 0
            End Select
        Next
	End Sub
	Private Function ReceiveCheckSum() As Boolean
		Dim i As Byte
		Dim sum As Integer
		Dim sumArray() As Byte
		Dim calcChecksum As Byte
		sum = 0
		For i = 0 To ReceiveLen - 1
			sum = sum + Receive(i)
		Next
		sumArray = BitConverter.GetBytes(sum)
		calcChecksum = &HFF - sumArray(0)
		If calcChecksum = ReceiveCheck Then
			ReceiveCheckSum = True
		Else
			ReceiveCheckSum = False
		End If
	End Function

	Private Function ReceiveBufferIsEmpy() As Boolean
		If DataReceiveBufferReadIndex = DataReceiveBufferWriteIndex Then
			ReceiveBufferIsEmpy = True
		Else
			ReceiveBufferIsEmpy = False
		End If
	End Function
	Private Function ReceiveBufferIsFull() As Boolean
		Dim temp As Byte
		temp = DataReceiveBufferWriteIndex + 1
		If temp > (MAX_BUFFER_SIZE - 1) Then temp = 0
		If temp = DataReceiveBufferReadIndex Then
			ReceiveBufferIsFull = True
		Else
			ReceiveBufferIsFull = False
		End If
	End Function
	Private Function ReceiveBufferPut() As Boolean
		Dim i As Byte
		Dim add(4) As Byte
		If ReceiveBufferIsFull() Then
			ReceiveBufferPut = False
			Exit Function
		End If
		add(0) = Receive(8)
		add(1) = Receive(7)
		add(2) = Receive(6)
		add(3) = Receive(5)
		DataReceiveBuffer(DataReceiveBufferWriteIndex).len = ReceiveLen - 12
		DataReceiveBuffer(DataReceiveBufferWriteIndex).ID = BitConverter.ToUInt32(add, 0)
		For i = 0 To DataReceiveBuffer(DataReceiveBufferWriteIndex).len - 1
			DataReceiveBuffer(DataReceiveBufferWriteIndex).data(i) = Receive(12 + i)
		Next
		DataReceiveBufferWriteIndex += 1
		If DataReceiveBufferWriteIndex = MAX_BUFFER_SIZE Then DataReceiveBufferWriteIndex = 0
		ReceiveBufferPut = True
	End Function

	Private Function TransmitBufferIsEmpy() As Boolean
		If DataTransmitBufferReadIndex = DataTransmitBufferWriteIndex Then
			TransmitBufferIsEmpy = True
		Else
			TransmitBufferIsEmpy = False
		End If
	End Function

	Private Function TransmitBufferIsFull() As Boolean
		Dim temp As Byte
		temp = DataTransmitBufferWriteIndex + 1
		If temp > (MAX_BUFFER_SIZE - 1) Then temp = 0
		If temp = DataTransmitBufferReadIndex Then
			TransmitBufferIsFull = True
		Else
			TransmitBufferIsFull = False
		End If
	End Function
	Private Function TransmitBufferGet() As XBeeDataStruct
		Dim XBeeData As XBeeDataStruct = New XBeeDataStruct()
		If TransmitBufferIsEmpy() Then
			Return Nothing
		End If
		If IsNothing(XBeeData) Then XBeeData = New XBeeDataStruct(0)
		XBeeData.ID = DataTransmitBuffer(DataTransmitBufferReadIndex).ID
		XBeeData.len = DataTransmitBuffer(DataTransmitBufferReadIndex).len
		XBeeData.data = DataTransmitBuffer(DataTransmitBufferReadIndex).data
		DataTransmitBufferReadIndex += 1
		If DataTransmitBufferReadIndex = MAX_BUFFER_SIZE Then DataTransmitBufferReadIndex = 0
		Return XBeeData
	End Function

	''' <summary>
	''' Set port for Xbee
	''' </summary>
	''' <param name="name">Port name</param>
	''' <param name="BaudRate">Port's Baudrate</param>
	''' <remarks></remarks>
	Public Sub SettingPort(ByVal name As String, ByVal BaudRate As Integer)
		If IsNothing(port) Then port = New SerialPort()
        If port.IsOpen Then port.Close()
		Try
			port.PortName = name
			port.BaudRate = BaudRate
			port.Open()
			AddHandler port.DataReceived, AddressOf Port_DataReceived
		Catch ex As Exception
			MessageBox.Show(ex.Message, "ERROR when create port for XBee", MessageBoxButtons.OK, MessageBoxIcon.Error)
		End Try
	End Sub

	Public Sub ClosePort()
		If IsNothing(port) Then Return
		port.Close()
	End Sub
	Private Xbee_isKbhit As Boolean = False
	''' <summary>
	''' Gets a value indicating the status of receive buffer in Xbee object
	''' </summary>
	''' <returns>Return true if there are some data in receive buffer, if receive buffer is empty, return false</returns>
	Public Function kbhit() As Boolean
		Return Xbee_isKbhit
	End Function
	''' <summary>
	''' Gets a value indicating the status of transmit buffer in Xbee object
	''' </summary>
	''' <returns>Return true if transmit buffer isn't full, it mean we can put more data to transmit buffer, if transmit buffer is full, return false</returns>
	Public Function tbe() As Boolean
		Return Not TransmitBufferIsFull()
	End Function
	''' <summary>
	''' Put the specified data to transmit buffer in Xbee
	''' </summary>
	''' <param name="XbeeData">The data was been send</param>
	''' <returns>Return true if success, false if it failure (Transmit buffer is full) </returns>
	''' <remarks></remarks>
	Public Function putd(ByVal XbeeData As XBeeDataStruct) As Boolean
		If TransmitBufferIsFull() Then
			Return False
		End If
		DataTransmitBuffer(DataTransmitBufferWriteIndex) = XbeeData
		DataTransmitBufferWriteIndex += 1
		If DataTransmitBufferWriteIndex = MAX_BUFFER_SIZE Then DataTransmitBufferWriteIndex = 0
		TransmitCounter.Enabled = True

		Return (True)
	End Function
	''' <summary>
	''' Get the data from receive buffer of Xbee object
	''' </summary>
	''' <returns>Return data if success, return nothing if failure</returns>
	''' <remarks></remarks>
	Public Function getd() As XBeeDataStruct
		Dim XbeeData As XBeeDataStruct = New XBeeDataStruct(0)
		If ReceiveBufferIsEmpy() Then
			Xbee_isKbhit = False
			TimerReceiveCheck.Enabled = False
			getd = Nothing
			Exit Function
		End If
		XbeeData.ID = DataReceiveBuffer(DataReceiveBufferReadIndex).ID
		XbeeData.len = DataReceiveBuffer(DataReceiveBufferReadIndex).len
		XbeeData.data = DataReceiveBuffer(DataReceiveBufferReadIndex).data
		DataReceiveBufferReadIndex += 1
		If DataReceiveBufferReadIndex = MAX_BUFFER_SIZE Then DataReceiveBufferReadIndex = 0
		getd = XbeeData
	End Function

	Private Sub TransmitCounter_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TransmitCounter.Elapsed
		If TransmitBufferIsEmpy() Then
			TransmitCounter.Enabled = False
			Return
		End If
		Dim XBeeData As XBeeDataStruct = New XBeeDataStruct()
		XBeeData = TransmitBufferGet()
		SendData(XBeeData.ID, XBeeData.data, XBeeData.len)
	End Sub

	Delegate Sub dlgTransmitTimerEnable(ByVal enable As Boolean)
	Private Sub TransmitTimerEnable(ByVal enable As Boolean)
		TransmitCounter.Start()
	End Sub


	Private Sub analizeInformationReceived()
		Select Case Receive(0)
			Case RFID_command.RECEIVE
				If Not ReceiveBufferIsFull() Then
					ReceiveBufferPut()
					TimerReceiveCheck.Enabled = True
					Xbee_isKbhit = True
					If ChildComponent.Count <> 0 Then
						While Xbee_isKbhit
							Dim receiveData As XBeeDataStruct
							Dim ob As XbeeDevices
							receiveData = getd()
							For Each ob In ChildComponent
                                If ob.getAddress = receiveData.ID Then
                                    ob.analizeInput(receiveData.ID, receiveData.data, receiveData.len)
                                    Exit For
                                End If
							Next
						End While
					End If
				End If
			Case RFID_command.RESPOND
				Dim AT_Command As Int16
				AT_Command = Receive(2) * 256 + Receive(3)
				Select Case AT_Command
					Case AT_COMMAND_ENUM.SERIAL_NUMBER_LOW
						Dim add As Byte() = New Byte(3) {Receive(8), Receive(7), Receive(6), Receive(5)}
						Address = BitConverter.ToUInt32(add, 0)
					Case AT_COMMAND_ENUM.PAN_ID
						Dim pan_id As Byte() = New Byte(1) {Receive(6), Receive(5)}
						PID = BitConverter.ToInt16(pan_id, 0)
				End Select
		End Select
	End Sub
	Public Sub Send_AT_Command(ByVal cmd As AT_COMMAND_ENUM)
		Dim data As Byte()
		Dim command As Byte() = BitConverter.GetBytes(cmd)
		data = New Byte(7) {0, 0, 0, 0, 0, 0, 0, 0}
		data(0) = &H7E
		data(1) = 0
		data(2) = 4
		data(3) = &H8
		data(4) = &H1
		data(5) = command(1)
		data(6) = command(0)

		Dim i As Byte
		Dim sum As Integer
		Dim sumArray() As Byte
		Dim len As Byte
		len = 4
		sum = 0
		For i = 3 To len + 2
			sum = sum + data(i)
		Next
		sumArray = BitConverter.GetBytes(sum)
		data(len + 3) = &HFF - sumArray(0)

		port.Write(data, 0, 8)

	End Sub

End Class

Public Interface XbeeDevices
    Sub analizeInput(ByVal ID As UInt32, ByVal data() As Byte, ByVal len As Byte)
    Function getAddress() As UInt32
End Interface