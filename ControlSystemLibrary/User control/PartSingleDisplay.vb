Imports System.Drawing
Imports System.Windows.Forms

Public Class PartSingleDisplay
    Private _AGVSupply As String
    Private _status As Boolean = False
    Private _connectStatus As Boolean = False
    Private _used As Boolean
    Property TotalCount As Integer
    Private _CurrentCount As Integer
    Property CurrentCount As Integer
        Get
            Return _CurrentCount
        End Get
        Set(value As Integer)
            _CurrentCount = value
            labelCount.Text = _CurrentCount.ToString + "/" + TotalCount.ToString
        End Set
    End Property
    Property AGVSupply As String
        Get
            Return _AGVSupply
        End Get
        Set(value As String)
            _AGVSupply = value
            If _used = False Then
                labelAGV.Text = ""
                labelCount.Text = ""
                SetBackground(Color.WhiteSmoke)
            Else
                labelAGV.Text = value
                If _connectStatus Then      'If status is connecting
                    Enabled = True
                    If value = "" Then      'If no AGV supply
                        If _status Then     'If part full
                            SetBackground(Color.Green)
                        Else
                            SetBackground(Color.Red)
                        End If
                    Else
                        SetBackground(Color.Yellow)
                    End If
                Else
                    Enabled = False
                    SetBackground(Color.LightGray)
                End If
            End If
        End Set
    End Property
    Property status As Boolean
        Get
            Return _status
        End Get
        Set(value As Boolean)
            _status = value
            If value Then
                AGVSupply = ""
            Else
                AGVSupply = _AGVSupply
            End If
        End Set
    End Property
    Property ConnectStatus As Boolean
        Get
            Return _connectStatus
        End Get
        Set(value As Boolean)
            _connectStatus = value
            AGVSupply = _AGVSupply
        End Set
    End Property
    Property Used As Boolean
        Get
            Return _used
        End Get
        Set(value As Boolean)
            _used = value
            AGVSupply = _AGVSupply
        End Set
    End Property
    Private Sub PartSingleDisplay_Load(sender As Object, e As EventArgs) Handles Me.Load
        TableLayoutPanel1.Dock = DockStyle.Fill
        labelName.Dock = DockStyle.Fill
        labelAGV.Dock = DockStyle.Fill
        labelCount.Dock = DockStyle.Fill
    End Sub

    Private Sub UserControl1_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        If Width < 100 Then
            Width = 100
        End If
        If Height < 100 Then
            Height = 100
        End If
    End Sub

    Private Sub SetBackground(c As Color)
        labelName.BackColor = c
        labelAGV.BackColor = c
        labelCount.BackColor = c
        BackColor = c
    End Sub

    Public Sub setDataBinding(myEndDevices As EndDevices, partNum As Byte)
        Me.DataBindings.Add(New Binding("Used", myEndDevices.Parts(partNum), "Enable"))
        Me.DataBindings.Add(New Binding("PartName", myEndDevices.Parts(partNum), "Name"))
        Me.DataBindings.Add(New Binding("ConnectStatus", myEndDevices, "connecting"))
        Me.DataBindings.Add(New Binding("PartStatus", myEndDevices.Parts(partNum), "Status"))
        Me.DataBindings.Add(New Binding("AGVSupply", myEndDevices.Parts(partNum), "AGVSupply"))
    End Sub
    Property PartName As String
        Get
            Return labelName.Text
        End Get
        Set(value As String)
            labelName.Text = value
        End Set
    End Property
End Class
