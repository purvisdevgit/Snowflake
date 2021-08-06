Module IdGenerate

    Sub Main()
        Dim idworker As IdWorker = New IdWorker(1)
        For i As Integer = 0 To 1000 - 1
            Console.WriteLine(idworker.nextId())
        Next
        Console.ReadKey(True)
    End Sub

End Module
