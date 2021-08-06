Public Class IdWorker
    '机器ID
    Private Shared workerId As Long
    Private Shared twepoch As Long = 687888001020L '唯一时间，这是一个避免重复的随机量，自行设定不要大于当前时间戳
    Private Shared sequence As Long = 0L
    Private Shared workerIdBits As Integer = 4 '机器码字节数。4个字节用来保存机器码(定义为Long类型会出现，最大偏移64位，所以左移64位没有意义)
    Public Shared maxWorkerId As Integer = -1L Xor -1L << workerIdBits '最大机器ID
    Private Shared sequenceBits As Integer = 10 '计数器字节数，10个字节用来保存计数码
    Private Shared workerIdShift As Integer = sequenceBits '机器码数据左移位数，就是后面计数器占用的位数
    Private Shared timestampLeftShift As Integer = sequenceBits + workerIdBits '时间戳左移动位数就是机器码和计数器总字节数
    Public Shared sequenceMask As Long = -1L Xor -1L << sequenceBits '一微秒内可以产生计数，如果达到该值则等到下一微妙在进行生成
    Private Shared lastTimestamp As Long = -1L
    ''' <summary>
    ''' 机器码
    ''' </summary>
    ''' <param name="workerId"></param>
    Public Sub New(ByVal workerId As Long)
        If workerId > maxWorkerId OrElse workerId < 0 Then Throw New Exception(String.Format("worker Id can't be greater than {0} or less than 0 ", workerId))
        IdWorker.workerId = workerId
    End Sub
    ''' <summary>
    ''' 雪花❄算法构建ID
    ''' </summary>
    ''' <returns></returns>
    Public Function nextId() As Long
        SyncLock Me
            Dim timestamp As Long = timeGen()
            If lastTimestamp = timestamp Then
                '同一微妙中生成ID
                IdWorker.sequence = (IdWorker.sequence + 1) And IdWorker.sequenceMask '用&运算计算该微秒内产生的计数是否已经到达上限
                If IdWorker.sequence = 0 Then
                    '一微妙内产生的ID计数已达上限，等待下一微妙
                    timestamp = tillNextMillis(lastTimestamp)
                End If
            Else
                '不同微秒生成ID
                IdWorker.sequence = 0 '计数清0
            End If
            If timestamp < lastTimestamp Then
                '如果当前时间戳比上一次生成ID时时间戳还小，抛出异常，因为不能保证现在生成的ID之前没有生成过
                Throw New Exception(String.Format("Clock moved backwards.  Refusing to generate id for {0} milliseconds", lastTimestamp - timestamp))
            End If
            lastTimestamp = timestamp '把当前时间戳保存为最后生成ID的时间戳
            Dim nextIdL As Long = (timestamp - twepoch << timestampLeftShift) Or IdWorker.workerId << IdWorker.workerIdShift Or IdWorker.sequence
            Return nextIdL
        End SyncLock
    End Function

    ''' <summary>
    ''' 获取下一微秒时间戳
    ''' </summary>
    ''' <param name="lastTimestamp"></param>
    ''' <returns></returns>
    Private Function tillNextMillis(ByVal lastTimestamp As Long) As Long
        Dim timestamp As Long = timeGen()
        While timestamp <= lastTimestamp
            timestamp = timeGen()
        End While
        Return timestamp
    End Function
    ''' <summary>
    ''' 生成当前时间戳
    ''' </summary>
    ''' <returns></returns>
    Private Function timeGen() As Long
        Return CLng((DateTime.UtcNow - New DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds)
    End Function
End Class
