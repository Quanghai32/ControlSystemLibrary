﻿Imports System.Timers

Public Class AGV
	Implements XbeeDevices
	Implements System.ComponentModel.INotifyPropertyChanged
	Private Event PropertyChanged(ByVal sender As Object, ByVal e As System.ComponentModel.PropertyChangedEventArgs) Implements System.ComponentModel.INotifyPropertyChanged.PropertyChanged

	Public myXbee As Xbee
	Private _RobocarStatusName As String() = New String(7) {"Unknown", "Emergency", "Safety", "Stop", "Out of line", "Battery Empty", "No Cart", "Normal"}
	''' <summary>
	''' Define the name of all status of AGV - Use with status property
	''' </summary>
	''' <returns>Return array of string: "Unknown", "Emergency", "Safety", "Stop", "Out of line", "Battery Empty", "No Cart", "Normal"</returns>
	ReadOnly Property RobocarStatusName As String()
		Get
			Return _RobocarStatusName
		End Get
	End Property

	Public Enum RobocarStatusValue
		UNKNOW = 0
		EMERGENCY = 1
		SAFETY = 2
		STOP_BY_CARD = 3
		OUT_OF_LINE = 4
		BATTERY_EMPTY = 5
		NO_CART = 6
		NORMAL = 7
	End Enum
	Private _RobocarWorkingStatusName As String() = New String(2) {"Free", "Supplying", "Returning"}
	ReadOnly Property RobocarWorkingStatusName As String()
		Get
			Return _RobocarWorkingStatusName
		End Get
	End Property

	Public Enum RobocarWorkingStatusValue
		FREE = 0
		SUPPLYING = 1
		RETURNING = 2
	End Enum

	Property Address As UInt32
	Property PID As Byte

	Private WithEvents timerDisconnect As Timer
	Private WithEvents timerFree As Timer
    Private BeingStartPoint As Boolean = False
    Private _index As Integer
    Private _group As Byte
	Private _Enable As Boolean
	Private _Name As String = "AGV"
	Private _Battery As Byte() = New Byte(3) {0, 0, 0, 0}
	Private _SupplyPartStatus As Byte = 0
	Private _Connecting As Boolean = False
	Private _Status As RobocarStatusValue
	Private _WorkingStatus As RobocarWorkingStatusValue = RobocarWorkingStatusValue.SUPPLYING
    Private _Position As Integer
    Private _TIMEOUT As Integer = 4000
    Private _TIME_FREE As Integer = 5000

    Property index As Integer
        Get
            Return _index
        End Get
        Set(value As Integer)
            _index = value
        End Set
    End Property
	''' <summary>
    ''' ***Get or set using status of AGV***
	''' </summary>
	''' <value>If the value is true, it mean AGV was used, if it's false, it mean AGV was not used</value>
	''' <returns>Return using state of AGV</returns>
	Property Enable As Boolean
		Get
			Return _Enable
		End Get
		Set(ByVal value As Boolean)
			_Enable = value
			RaiseEvent PropertyChanged(Me, New System.ComponentModel.PropertyChangedEventArgs("Enable"))
		End Set
	End Property
	''' <summary>
    ''' ***Get or set name of AGV***
	''' </summary>
	''' <value>Set new name for AGV</value>
	''' <returns>Return the current name of AGV</returns>
	Property Name As String
		Get
			Return _Name
		End Get
		Set(ByVal value As String)
			_Name = value
			RaiseEvent PropertyChanged(Me, New System.ComponentModel.PropertyChangedEventArgs("Name"))
		End Set
    End Property
    Property group As Byte
        Get
            Return _group
        End Get
        Set(value As Byte)
            _group = value
        End Set
    End Property
	''' <summary>
    ''' ***Set or get array of battery value***
	''' </summary>
	''' <returns>Return array of battery value</returns>
	''' <remarks>The battery value's size is one byte</remarks>
	Property Battery As Byte()
		Get
			Return _Battery
		End Get
		Set(ByVal value As Byte())
			Dim i As Byte
			For i = 0 To 3
				_Battery(i) = value(i)
			Next
			RaiseEvent PropertyChanged(Me, New System.ComponentModel.PropertyChangedEventArgs("Battery"))
		End Set
	End Property
	''' <summary>
    ''' ***Set new route (Part) or get current route (part) of AGV***
	''' </summary>
	''' <returns>Return the current route number of AGV</returns>
	Property SupplyPartStatus As Byte
		Get
			Return _SupplyPartStatus
		End Get
		Set(ByVal value As Byte)
			_SupplyPartStatus = value
			RaiseEvent PropertyChanged(Me, New System.ComponentModel.PropertyChangedEventArgs("SupplyPartStatus"))
		End Set
	End Property
	''' <summary>
    ''' ******
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Property Connecting As Boolean
		Get
			Return _Connecting
		End Get
		Set(ByVal value As Boolean)
			_Connecting = value
			RaiseEvent PropertyChanged(Me, New System.ComponentModel.PropertyChangedEventArgs("Connecting"))
		End Set
	End Property
	Property Status As RobocarStatusValue
		Get
			Return _Status
		End Get
		Set(ByVal value As RobocarStatusValue)
			_Status = value
			RaiseEvent PropertyChanged(Me, New System.ComponentModel.PropertyChangedEventArgs("Status"))
		End Set
	End Property
	Property WorkingStatus As RobocarWorkingStatusValue
		Get
			Return _WorkingStatus
		End Get
		Set(ByVal value As RobocarWorkingStatusValue)
			_WorkingStatus = value
			RaiseEvent PropertyChanged(Me, New System.ComponentModel.PropertyChangedEventArgs("WorkingStatus"))
		End Set
	End Property
    Property Position As Integer
        Get
            Return _Position
        End Get
        Set(ByVal value As Integer)
            _Position = value
            RaiseEvent PropertyChanged(Me, New System.ComponentModel.PropertyChangedEventArgs("Position"))
        End Set
    End Property
    Property TIMEOUT As Integer
        Get
            Return _TIMEOUT
        End Get
        Set(value As Integer)
            _TIMEOUT = value
            timerDisconnect.Interval = _TIMEOUT
        End Set
    End Property
    Property TIME_FREE As Integer
        Get
            Return _TIME_FREE
        End Get
        Set(value As Integer)
            _TIME_FREE = value
            timerFree.Interval = _TIME_FREE
        End Set
    End Property

    Public SupplyTime As DateTime = Now
    Public FreeTime As DateTime = Now

	''' <summary>
	''' Create and return new AGV object, using the specified name
	''' </summary>
	''' <param name="name">Name of AGV</param>
	Public Sub New(ByVal name As String)
		MyClass.Name = name
		init()
	End Sub
	''' <summary>
	''' Create and return new AGV object, using the specified name and address
	''' </summary>
	''' <param name="name">Name of AGV</param>
	''' <param name="address">Address of AGV (32 bits number)</param>
	Public Sub New(ByVal name As String, ByVal address As UInt32)
		MyClass.Name = name
		MyClass.Address = address
		init()
	End Sub

	Private Sub init()
		timerDisconnect = New Timer()
		timerDisconnect.Interval = TIMEOUT
		startTimer()
		timerFree = New Timer()
        timerFree.Interval = TIME_FREE
	End Sub

	Private Sub startTimer()
		timerDisconnect.Start()
	End Sub
	Private Sub stopTimer()
		timerDisconnect.Stop()
	End Sub

    Friend Sub analizeInput(ByVal ID As UInt32, ByVal data() As Byte, ByVal len As Byte) Implements XbeeDevices.analizeInput
        If ID <> Address Then Return
        If len <> 8 Then Return 'Standard about length of data is 8
        '"7-Battery 4 | 6-Battery 3 | 5-Battery 2 | 4-Battery 1 | 3-SupplyPartStatus | 2-Position 0 | 1-Position 1 | 0-Status"
        Connecting = True
        timerDisconnect.Stop()
        timerDisconnect.Start()
        Status = data(0)
        Position = (CType(data(1), Integer) << 8) + data(2)
        If WorkingStatus <> RobocarWorkingStatusValue.FREE Then
            If Status = RobocarStatusValue.STOP_BY_CARD Then
                If Not BeingStartPoint Then
                    If isInStartPoint() Then
                        BeingStartPoint = True
                        timerFree.Start()
                    Else
                        timerFree.Stop()
                    End If
                End If
            End If
        ElseIf Status <> RobocarStatusValue.STOP_BY_CARD Then
            BeingStartPoint = False
            timerFree.Stop()
            WorkingStatus = RobocarWorkingStatusValue.SUPPLYING
        End If
		SupplyPartStatus = data(3)
        Dim i As Byte
        For i = 0 To 3
            Battery(i) = data(4 + i)
		Next
	End Sub
	Friend Function getAdress() As UInt32 Implements XbeeDevices.getAddress
		Return Address
	End Function

	Private Sub timerDisconnect_Elapsed(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles timerDisconnect.Elapsed
		Connecting = False
	End Sub
	Private Sub timerFree_Elapsed(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles timerFree.Elapsed
		BeingStartPoint = False
        If Status = RobocarStatusValue.STOP_BY_CARD Then
            WorkingStatus = RobocarWorkingStatusValue.FREE
            FreeTime = Now
        End If
        timerFree.Stop()
	End Sub
	''' <summary>
	''' Request AGV run in new route
	''' </summary>
	''' <param name="Route">New route number</param>
	Public Sub RequestRoute(ByVal Route As Byte)
		Dim data As XBee.XBeeDataStruct = New XBee.XBeeDataStruct(0)
		data.ID = Address
		data.data(0) = &H57	'Write command - 'W'
		data.data(1) = &H52	'Run command - 'R'
        data.data(2) = Route
		data.len = 3
		myXbee.putd(data)
	End Sub
	''' <summary>
	''' Request AGV run with preview route
	''' </summary>
	Public Sub AGVRun()
		Dim data As XBee.XBeeDataStruct = New XBee.XBeeDataStruct(0)
		data.ID = Address
		data.data(0) = &H57	'Write command - 'W'
		data.data(1) = &H52	'Run command - 'R'
		data.data(2) = &HFF	'Don't change route
		data.len = 3
		myXbee.putd(data)
	End Sub
	''' <summary>
	''' Request AGV stop
	''' </summary>
	Public Sub AGVStop()
		Dim data As XBee.XBeeDataStruct = New XBee.XBeeDataStruct(0)
		data.ID = Address
		data.data(0) = &H57	'Write command - 'W'
		data.data(1) = &H53	'Stop command - 'S'
		data.len = 2
		myXbee.putd(data)
	End Sub
	''' <summary>
	''' Request AGV remember address of Xbee (In case AGV was been changed)
	''' </summary>
	''' <remarks>When user use new AGV, because AGV allways send data to control system, so that we must set the address of Xbee, which control this AGV, for this AGV</remarks>
	Public Sub SetHostAddress()
		Dim data As XBee.XBeeDataStruct = New XBee.XBeeDataStruct(0)
		data.ID = Address
		data.data(0) = &H53	'Setting command - 'S'
		data.len = 1
		myXbee.putd(data)
	End Sub

	Private Function isInStartPoint() As Boolean
		Dim i As Byte
		For i = 0 To StartPoint.Length - 1
			If _Position = StartPoint(i) Then
				Return True
			End If
		Next
		Return False
    End Function
    Public Function BatteryPercent() As Byte
        Dim value As Byte = Battery(0)
        For i As Byte = 0 To 3
            If value > Battery(i) Then value = Battery(i)
        Next
        Dim rt As Single = (value - 110) / 20 * 100
        If rt < 0 Then
            rt = 0
        ElseIf rt > 100 Then
            rt = 100
        End If
        Return Byte.Parse(rt)
    End Function
End Class
