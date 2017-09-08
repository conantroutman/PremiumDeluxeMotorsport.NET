﻿Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports GTA
Imports GTA.Native
Imports GTA.Math
Imports GTA.Game
Imports INMNativeUI
Imports System.Reflection
Imports PDMCD4.Resources

Public Class PDM
    Inherits Script

    Public Shared playerHash, SelectedVehicle As String, PlayerCash, VehiclePrice As Integer, VehPreview As Vehicle, PdmBlip As Blip
    Public Shared Price As Decimal = 0, Radius As Integer = 120, TestDrive As Integer = 1, VehicleName As String = Nothing
    Public Shared HideHud As Boolean = False, DrawSpotLight As Boolean = False, VehicleSpin As Boolean = True, ShowVehicleName As Boolean = False
    Public Shared PdmDoor, testDriveVector, VehPreviewPos As Vector3, GPC As Ped, GP As Player
    Public Shared CameraPos, CameraRot As Vector3
    Public Shared PlayerHeading, PdmDoorDist, TestDriveDist As Single
    Public Shared camera As WorkshopCamera
    Public Shared cutCamera As Camera
    Public Shared pdmIntID As Integer
    Public Shared lastVehMemory As Memory
    Public Shared TaskScriptStatus As Integer = -1
    Public Shared pdmStats As Scaleform = New Scaleform("mp_car_stats_02")
    Public Shared zTimer As Timer = New Timer(10000)
    Public Shared pdmPed As Ped

    Public Sub New()
        Try
            PDMMenu.LoadSettings()

            pdmIntID = GetInteriorID(New Vector3(-44.45501, -1096.976, 26.42235))
            playerHash = Game.Player.Character.Model.GetHashCode().ToString
            GP = Game.Player
            GPC = Game.Player.Character
            Select Case playerHash
                Case "1885233650"
                    PlayerCash = 1999999999
                Case "-1667301416"
                    PlayerCash = 1999999999
                Case Else
                    PlayerCash = Game.Player.Money
            End Select

            camera = New WorkshopCamera

            VehPreviewPos = New Vector3(-44.45501, -1096.976, 26.42235)
            CameraPos = New Vector3(-47.45673, -1101.28, 27.54757)
            CameraRot = New Vector3(-18.12634, 0, -26.97177)
            PlayerHeading = 250.6701
            ToggleIPL("shr_int", "fakeint")
            LoadMissingProps()

            CreateEntrance()
        Catch ex As Exception
            logger.Log(ex.Message & " " & ex.StackTrace)
        End Try
    End Sub

    Public Shared Sub CreateEntrance()
        PdmDoor = New Vector3(-55.99228, -1098.51, 25.423)
        testDriveVector = New Vector3(66.55125, -1356.585, 29.08711)
        PdmBlip = World.CreateBlip(PdmDoor)
        PdmBlip.Sprite = BlipSprite.PersonalVehicleCar
        PdmBlip.Color = BlipColor.Red
        PdmBlip.IsShortRange = True
        PdmBlip.Name = GetLangEntry("PREMIUM_DELUXE_MOTORSPORT")
    End Sub

    Public Shared Sub OnTick(o As Object, e As EventArgs) Handles Me.Tick
        Try
            If MissionFlag Or GP.WantedLevel > 1 Then
                PdmBlip.Alpha = 0
            Else
                PdmBlip.Alpha = 255
            End If
        Catch ex As Exception
            logger.Log("Error Blip Visible " & ex.Message & " " & ex.StackTrace)
        End Try

        Try
            pdmIntID = GetInteriorID(New Vector3(-44.45501, -1096.976, 26.42235))
            PdmDoorDist = World.GetDistance(GPC.Position, PdmDoor)
            TestDriveDist = World.GetDistance(GPC.Position, testDriveVector)
            playerHash = Game.Player.Character.Model.GetHashCode().ToString
            GP = Game.Player
            GPC = Game.Player.Character
            Select Case playerHash
                Case "1885233650"
                    PlayerCash = 1999999999
                Case "-1667301416"
                    PlayerCash = 1999999999
                Case Else
                    PlayerCash = Game.Player.Money
            End Select
        Catch ex As Exception
            logger.Log("Error Get Info " & ex.Message & " " & ex.StackTrace)
        End Try

        Try
            If PdmDoorDist < 10.0 Then
                If pdmPed = Nothing Then
                    Dim chairs As Prop() = World.GetNearbyProps(PdmDoor, 3.0, "v_corp_offchair")
                    Dim chair As Prop = Nothing
                    For Each props As Prop In chairs
                        chair = props
                        chair.FreezePosition = True
                    Next
                    pdmPed = World.CreatePed(PedHash.CarDesignFemale01, PdmDoor, 219.5891)
                    pdmPed.IsPersistent = True
                    pdmPed.Task.StartScenario("PROP_HUMAN_SEAT_CHAIR_UPRIGHT", New Vector3(chair.Position.X, chair.Position.Y, chair.Position.Z + 0.46))
                    'AttachTo(pdmPed, chair, 0, New Vector3(0, 0, 1), Vector3.Zero)
                End If
                pdmPed.Task.LookAt(GPC)
                pdmPed.AlwaysKeepTask = True
            End If
        Catch ex As Exception

        End Try

        Try
            If Not GPC.IsInVehicle AndAlso Not GPC.IsDead AndAlso PdmDoorDist < 3.0 AndAlso GP.WantedLevel = 0 AndAlso TaskScriptStatus = -1 Then
                DisplayHelpTextThisFrame(GetLangEntry("HELP_ENTER_SHOP"))
            ElseIf Not GPC.IsInVehicle AndAlso Not GPC.IsDead AndAlso PdmDoorDist < 3.0 AndAlso GP.WantedLevel >= 1 Then
                Native.Function.Call(Hash.DISPLAY_HELP_TEXT_THIS_FRAME, New InputArgument() {"LOSE_WANTED", 0})
            End If

            If TestDrive = 3 AndAlso Not GPC.IsInVehicle Then
                FadeScreenOut(500)
                Wait(&H3E8)
                Dim penalty As Double = VehiclePrice / 99
                If VehPreview.HasBeenDamagedBy(GPC) Then
                    GP.Money = (PlayerCash - (VehiclePrice / 99))
                    DisplayHelpTextThisFrame("$" & Math.Round(penalty) & GetLangEntry("HELP_PENALTY"))
                    UI.Notify("$" & Math.Round(penalty) & GetLangEntry("HELP_PENALTY"))
                End If
                PDMMenu.ConfirmMenu.Visible = True
                VehPreview.IsDriveable = False
                VehPreview.LockStatus = VehicleLockStatus.CannotBeTriedToEnter
                VehPreview.Position = VehPreviewPos
                VehPreview.Heading = Radius
                Native.Function.Call(Hash.SET_VEHICLE_DOORS_SHUT, VehPreview, False)
                Native.Function.Call(Hash.SET_VEHICLE_FIXED, VehPreview)
                TestDrive = 1
                HideHud = True
                Wait(500)
                FadeScreenIn(500)
                ShowVehicleName = True
                camera.RepositionFor(VehPreview)
            ElseIf TestDrive = 3 AndAlso TestDriveDist > 450.0 Then
                FadeScreenOut(500)
                Wait(&H3E8)
                Dim penalty As Double = VehiclePrice / 99
                If VehPreview.HasBeenDamagedBy(GPC) Then
                    GP.Money = (PlayerCash - (VehiclePrice / 99))
                    UI.Notify("$" & Math.Round(penalty) & GetLangEntry("HELP_PENALTY"))
                End If
                PDMMenu.ConfirmMenu.Visible = True
                VehPreview.IsDriveable = False
                VehPreview.LockStatus = VehicleLockStatus.CannotBeTriedToEnter
                VehPreview.Position = VehPreviewPos
                VehPreview.Heading = Radius
                Native.Function.Call(Hash.SET_VEHICLE_DOORS_SHUT, VehPreview, False)
                Native.Function.Call(Hash.SET_VEHICLE_FIXED, VehPreview)
                TestDrive = 1
                HideHud = True
                Wait(500)
                FadeScreenIn(500)
                ShowVehicleName = True
                camera.RepositionFor(VehPreview)
            ElseIf TestDrive = 2 AndAlso GPC.IsInVehicle Then
                TestDrive = TestDrive + 1
            End If
        Catch ex As Exception
            logger.Log("Error Show Help " & ex.Message & " " & ex.StackTrace)
        End Try

        Try
            If HideHud Then
                'Native.Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME)
                Native.Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME)
                Native.Function.Call(Hash.SHOW_HUD_COMPONENT_THIS_FRAME, 3)
                Native.Function.Call(Hash.SHOW_HUD_COMPONENT_THIS_FRAME, 4)
                Native.Function.Call(Hash.SHOW_HUD_COMPONENT_THIS_FRAME, 5)
                Native.Function.Call(Hash.SHOW_HUD_COMPONENT_THIS_FRAME, 13)
                camera.Update()
            End If

            If DrawSpotLight = True Then
                World.DrawSpotLightWithShadow(VehPreviewPos + Vector3.WorldUp * 4 + Vector3.WorldNorth * 4, Vector3.WorldSouth + Vector3.WorldDown, Color.White, 30, 30, 100, 50, -1)
                World.DrawSpotLight(VehPreviewPos + Vector3.WorldUp * 4 + Vector3.WorldNorth * 4, Vector3.WorldSouth + Vector3.WorldDown, Color.White, 30, 30, 100, 50, -1)
            End If
        Catch ex As Exception
            logger.Log("Error Hud Spotlight " & ex.Message & " " & ex.StackTrace)
        End Try

        Try
            If IsControlJustPressed(0, GTA.Control.Context) AndAlso PdmDoorDist < 3.0 AndAlso Not GPC.IsInVehicle AndAlso GP.WantedLevel = 0 AndAlso TaskScriptStatus = -1 Then
                TaskScriptStatus = 0

                HideHud = True
                Wait(500)
                FadeScreenIn(500)
                SelectedVehicle = PDMMenu.optLastVehName
                If VehPreview = Nothing Then
                    VehPreview = CreateVehicleFromHash(PDMMenu.optLastVehHash, VehPreviewPos, 6.122209)
                Else
                    VehPreview.Delete()
                    VehPreview = CreateVehicleFromHash(PDMMenu.optLastVehHash, VehPreviewPos, 6.122209)
                End If
                PDMMenu.UpdateVehPreview()
                camera.RepositionFor(VehPreview)
                VehicleName = SelectedVehicle
                ShowVehicleName = True
                VehPreview.Heading = Radius
                VehPreview.LockStatus = VehicleLockStatus.CannotBeTriedToEnter
                VehPreview.DirtLevel = 0
                PDMMenu.MainMenu.Visible = True
            End If

            If IsControlJustReleased(0, GTA.Control.ParachuteBrakeLeft) AndAlso GetInteriorID(VehPreview.Position) = pdmIntID AndAlso Not GPC.IsInVehicle AndAlso TaskScriptStatus = 0 Then
                Native.Function.Call(Hash.SET_VEHICLE_DOORS_SHUT, VehPreview, False)
            ElseIf IsControlJustReleased(0, GTA.Control.ParachuteBrakeRight) AndAlso GetInteriorID(VehPreview.Position) = pdmIntID AndAlso Not GPC.IsInVehicle AndAlso TaskScriptStatus = 0 Then
                VehPreview.OpenDoor(VehicleDoor.BackLeftDoor, False, False)
                VehPreview.OpenDoor(VehicleDoor.BackRightDoor, False, False)
                VehPreview.OpenDoor(VehicleDoor.FrontLeftDoor, False, False)
                VehPreview.OpenDoor(VehicleDoor.FrontRightDoor, False, False)
                VehPreview.OpenDoor(VehicleDoor.Hood, False, False)
                VehPreview.OpenDoor(VehicleDoor.Trunk, False, False)
            ElseIf IsControlJustReleased(0, GTA.Control.ParachuteBrakeLeft) AndAlso GetInteriorID(VehPreview.Position) = pdmIntID AndAlso Not GPC.IsInVehicle AndAlso TaskScriptStatus = 0 Then
                VehicleSpin = True
            ElseIf IsControlJustReleased(0, GTA.Control.ParachuteBrakeRight) AndAlso GetInteriorID(VehPreview.Position) = pdmIntID AndAlso Not GPC.IsInVehicle AndAlso TaskScriptStatus = 0 Then
                VehicleSpin = True
            ElseIf IsControlJustPressed(0, GTA.Control.VehicleRoof) AndAlso GetInteriorID(VehPreview.Position) = pdmIntID AndAlso TaskScriptStatus = 0 Then
                If VehPreview.RoofState = VehicleRoofState.Closed Then
                    Native.Function.Call(Hash.LOWER_CONVERTIBLE_ROOF, VehPreview, False)
                Else
                    Native.Function.Call(Hash.RAISE_CONVERTIBLE_ROOF, VehPreview, False)
                End If
            End If

            If IsControlJustReleased(0, GTA.Control.VehicleHandbrake) AndAlso GetInteriorID(VehPreview.Position) = pdmIntID AndAlso Not GPC.IsInVehicle Then
                If camera.MainCameraPosition = CameraPosition.Car Then
                    camera.MainCameraPosition = CameraPosition.Interior
                Else
                    camera.MainCameraPosition = CameraPosition.Car
                End If
            End If
        Catch ex As Exception
            logger.Log("Error keypress " & ex.Message & " " & ex.StackTrace)
        End Try

        Try
            If ShowVehicleName = True AndAlso Not VehicleName = Nothing AndAlso GetInteriorID(VehPreview.Position) = pdmIntID AndAlso TaskScriptStatus = 0 Then
                Select Case Game.Language.ToString
                    Case "Chinese", "Korean", "Japanese"
                        DrawText(VehicleName, New Point(0, 500), 2.0, Color.White, GTAFont.UIDefault, GTAFontAlign.Right, GTAFontStyleOptions.DropShadow)
                        DrawText(GetClassDisplayName(VehPreview.ClassType), New Point(0, 550), 2.0, Color.DodgerBlue, GTAFont.Script, GTAFontAlign.Right, GTAFontStyleOptions.DropShadow)
                    Case Else
                        DrawText(VehicleName, New Point(0, 500), 2.0, Color.White, GTAFont.Title, GTAFontAlign.Right, GTAFontStyleOptions.DropShadow)
                        DrawText(GetClassDisplayName(VehPreview.ClassType), New Point(0, 550), 2.0, Color.DodgerBlue, GTAFont.Script, GTAFontAlign.Right, GTAFontStyleOptions.DropShadow)
                End Select

                If Game.IsControlJustPressed(0, GTA.Control.MultiplayerInfo) Then
                    zTimer.Start()
                End If

            End If

            If zTimer.Enabled Then
                PlayStatScaleform()

                If Game.GameTime > zTimer.Waiter Then
                    zTimer.Enabled = False
                    pdmStats.Dispose()
                End If
            End If
        Catch ex As Exception
            logger.Log("Error Spin Car Name " & ex.Message & " " & ex.StackTrace)
        End Try
    End Sub

    Public Shared Sub PlayStatScaleform()
        If Not pdmStats.IsLoaded Then pdmStats = New Scaleform("mp_car_stats_02")
        Dim menuLogo As String = PDMMenu.optLastVehMake
        Dim resLogo As String = Resources.GetCarMakeNames(VehPreview)
        Dim finalLogo As String = Nothing

        If menuLogo = Nothing Then
            finalLogo = resLogo
        Else
            finalLogo = menuLogo
        End If

        Dim acceleration As Integer = Native.Function.Call(Of Integer)(Hash.GET_VEHICLE_ACCELERATION, VehPreview)
        Dim braking As Integer = Native.Function.Call(Of Integer)(Hash.GET_VEHICLE_MAX_BRAKING, VehPreview)
        Dim traction As Integer = Native.Function.Call(Of Integer)(Hash.GET_VEHICLE_MAX_TRACTION, VehPreview)
        Dim topspeed As Integer = Native.Function.Call(Of Integer)(Hash._0x53AF99BAA671CA47, VehPreview)

        pdmStats.CallFunction("SET_VEHICLE_INFOR_AND_STATS", VehPreview.FriendlyName, Game.GetGXTEntry(finalLogo), "MPCarHUD", finalLogo, Game.GetGXTEntry("CMOD_STAT_0"), Game.GetGXTEntry("CMOD_STAT_1"), Game.GetGXTEntry("CMOD_STAT_2"), Game.GetGXTEntry("CMOD_STAT_3"), topspeed Mod 100, acceleration Mod 100, braking Mod 100, traction Mod 100) 'topspeed, acceleration, braking, traction
        pdmStats.Render3D(VehPreview.Position + New Vector3(0.0, 0.0, VehPreview.Model.GetDimensions().Z + 1.5), camera.Rotation, New Vector3(8, 4.5, 1))
        'UI.ShowSubtitle(String.Format("ACC: {0} | BRK: {1} | TRK: {2} | TSP: {3}", acceleration, braking, traction, topspeed))
    End Sub

    Public Sub OnAborted() Handles MyBase.Aborted
        Try
            PdmBlip.Remove()
            Game.FadeScreenIn(500)
            If Not pdmPed = Nothing Then pdmPed.Delete()
        Catch ex As Exception
            logger.Log(ex.Message & ex.StackTrace)
        End Try
    End Sub
End Class