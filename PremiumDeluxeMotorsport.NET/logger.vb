﻿Imports System.IO

Public NotInheritable Class logger

    Private Sub New()

    End Sub

    Public Shared Sub Log(message As Object)
        File.AppendAllText(".\PDMCarShop.log", DateTime.Now & ":" & message & Environment.NewLine)
    End Sub

End Class
