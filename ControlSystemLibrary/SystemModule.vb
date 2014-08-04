Public Module SystemModule
    Property StartPoint As Byte() = New Byte(1) {10, 20}
    ''' <summary>
    ''' Link AGV or End Devices and Xbee together
    ''' </summary>
    ''' <param name="userAGV">AGV object</param>
    ''' <param name="userXbee">Xbee which receive data from AGV</param>
    ''' <remarks></remarks>
    Public Sub LinkDeviceAndXbee(ByVal userAGV As AGV, ByVal userXbee As XBee)
        userXbee.ChildComponent.Add(userAGV)
        userAGV.myXbee = userXbee
    End Sub
    ''' <summary>
    ''' Link AGV or End Devices and Xbee together
    ''' </summary>
    ''' <param name="userEndDevices">End Devices</param>
    ''' <param name="userXbee">Xbee which receive data from AGV</param>
    ''' <remarks></remarks>
    Public Sub LinkDeviceAndXbee(ByVal userEndDevices As EndDevices, ByVal userXbee As XBee)
        userXbee.ChildComponent.Add(userEndDevices)
        userEndDevices.myXbee = userXbee
    End Sub
End Module
