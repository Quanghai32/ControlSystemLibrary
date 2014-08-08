Imports System.Timers
Public Class EndDevices
    Implements XbeeDevices
    Implements System.ComponentModel.INotifyPropertyChanged
    Property Address As UInt32
    Property PID As Byte
    Protected Property MAX_DEVICES As Byte

    Property TIMEOUT As Integer = 2000

    Private _connecting As Boolean
    Public Parts() As CPart
    Private WithEvents timerDisconnect As Timer

    Friend myXbee As XBee

    Property connecting As Boolean
        Get
            Return _connecting
        End Get
        Set(ByVal value As Boolean)
            _connecting = value
            RaiseEvent PropertyChanged(Me, New System.ComponentModel.PropertyChangedEventArgs("connecting"))
        End Set
    End Property

    Public Sub New()
        MAX_DEVICES = 0
        init()
    End Sub
    Public Sub New(ByVal DeviceLimit As Byte)
        MAX_DEVICES = DeviceLimit
        init()
    End Sub
    Public Sub New(ByVal DeviceLimit As Byte, ByVal add As UInt32)
        MAX_DEVICES = DeviceLimit
        Address = add
        init()
    End Sub

    Public Sub init()
        Dim i As Byte
        If MAX_DEVICES > 0 Then
            Parts = New CPart(MAX_DEVICES - 1) {}
            For i = 0 To MAX_DEVICES - 1
				Parts(i) = New CPart
				Parts(i).parent = Me
            Next
        End If
        timerDisconnect = New Timer()
        timerDisconnect.Interval = TIMEOUT
        startTimer()
    End Sub

    Private Sub startTimer()
        timerDisconnect.Start()
    End Sub
    Private Sub stopTimer()
        timerDisconnect.Stop()
    End Sub
    Friend Sub analizeInput(ByVal ID As UInt32, ByVal data() As Byte) Implements XbeeDevices.analizeInput
        Dim i As Byte
        If ID <> Address Then Exit Sub
        connecting = True
        timerDisconnect.Stop()
        timerDisconnect.Start()
        Static FullCounter As Integer = 0
        Static EmptyCounter As Integer = 0
        For i = 0 To MAX_DEVICES - 1
            If data(i) = 0 Then 'Sensor detect
                Parts(i).EmptyCounter = 0
                If Parts(i).TIME_FULL = 0 Then   'If confirm timer is disable
					Parts(i).Status = True
					Parts(i).AGVSupply = ""
                    Continue For
                End If
                If Parts(i).Status = True Then 'If preview status is full - Only update status again
                    Parts(i).Status = True
                Else    'If preview status is empty
                    If Parts(i).FullCounter = 0 Then 'if timer not set - it mean this's the first time sensor detect full
                        Parts(i).FullCounter = Environment.TickCount
                    Else
                        If Environment.TickCount > (Parts(i).FullCounter + Parts(i).TIME_FULL) Then
                            Parts(i).Status = True
                            Parts(i).FullCounter = 0
                        End If
                    End If
                End If
            Else
                Parts(i).FullCounter = 0
                If Parts(i).TIME_EMPTY = 0 Then
                    Parts(i).Status = False
                    Continue For
                End If
                If Parts(i).Status = False Then
                    Parts(i).Status = False
                Else
                    If Parts(i).EmptyCounter = 0 Then
                        Parts(i).EmptyCounter = Environment.TickCount
                    Else
                        If Environment.TickCount > (Parts(i).EmptyCounter + Parts(i).TIME_EMPTY) Then
                            Parts(i).Status = False
                            Parts(i).EmptyCounter = 0
                        End If
                    End If
                End If
			End If
			If Parts(i).Status = True Then
				Parts(i).AGVSupply = ""
			End If
        Next
    End Sub
    Sub timerDisconnect_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles timerDisconnect.Elapsed
        connecting = False
    End Sub
    Friend Function getAddress() As UInt32 Implements XbeeDevices.getAddress
        Return Address
    End Function

    Public Sub SettingAddress()
        Dim data As XBee.XBeeDataStruct = New XBee.XBeeDataStruct(0)
        data.ID = Address
        data.data(0) = &H53 'Setting command - 'S'
        data.len = 1
        myXbee.putd(data)
    End Sub

    Private Event PropertyChanged(ByVal sender As Object, ByVal e As System.ComponentModel.PropertyChangedEventArgs) Implements System.ComponentModel.INotifyPropertyChanged.PropertyChanged
End Class

Public Class CPart
    Implements System.ComponentModel.INotifyPropertyChanged
    Private _Enable As Boolean
    Private _Name As String
    Private _Status As Boolean
    Private _index As Byte
    Private _priority As Byte
    Private _group As Byte
    Property TIME_FULL As Integer = 0
    Property TIME_EMPTY As Integer = 0
    Friend EmptyCounter As Integer = 0
    Friend FullCounter As Integer = 0
	Public parent As EndDevices
	Public isRequested As Boolean = False
	Public EmptyTime As DateTime = New DateTime()
    Property Enable As Boolean
        Get
            Return _Enable
        End Get
        Set(ByVal value As Boolean)
            _Enable = value
            RaiseEvent PropertyChanged(Me, New System.ComponentModel.PropertyChangedEventArgs("Enable"))
        End Set
    End Property
    Property Name As String
        Get
            Return _Name
        End Get
        Set(ByVal value As String)
            _Name = value
            RaiseEvent PropertyChanged(Me, New System.ComponentModel.PropertyChangedEventArgs("Name"))
        End Set
    End Property
    Property Status As Boolean
        Get
            Return _Status
        End Get
        Set(ByVal value As Boolean)
            _Status = value
            RaiseEvent PropertyChanged(Me, New System.ComponentModel.PropertyChangedEventArgs("Status"))
        End Set
    End Property
    Property index As Byte
        Get
            Return _index
        End Get
        Set(value As Byte)
            _index = value
        End Set
    End Property
    Private _AGVSupply As String
    Public Property AGVSupply() As String
        Get
            Return _AGVSupply
        End Get
        Set(ByVal value As String)
            _AGVSupply = value
            RaiseEvent PropertyChanged(Me, New System.ComponentModel.PropertyChangedEventArgs("AGVSupply"))
        End Set
    End Property
    Property priority As Byte
        Get
            Return _priority
        End Get
        Set(value As Byte)
            _priority = value
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
    Private Event PropertyChanged(ByVal sender As Object, ByVal e As System.ComponentModel.PropertyChangedEventArgs) Implements System.ComponentModel.INotifyPropertyChanged.PropertyChanged
End Class