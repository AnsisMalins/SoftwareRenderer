using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// Unity's implementation of ThreadPool allocates 1 KB per call to QueueUserWorkItem, so we have to
// roll our own.

public static class ThreadPool
{
    private static Queue<WaitCallback> queue;
    private static int waiterCount;
    private static EventWaitHandle waitHandle;

    static ThreadPool()
    {
        queue = new Queue<WaitCallback>();
        waiterCount = Mathf.Max(SystemInfo.processorCount - 1, 1);
        waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        for (int i = 0; i < waiterCount; i++)
        {
            var thread = new Thread(ThreadProc);
            thread.IsBackground = true;
            thread.Start();
        }
    }

    public static bool QueueUserWorkItem(WaitCallback callBack)
    {
        if (callBack == null)
            throw new ArgumentNullException("callBack");
        lock (queue)
            queue.Enqueue(callBack);
        if (waiterCount > 0)
            waitHandle.Set();
        return true;
    }

    private static void ThreadProc()
    {
        while (true)
        {
            waitHandle.WaitOne();
            Interlocked.Decrement(ref waiterCount);
            while (true)
            {
                WaitCallback callBack;
                lock (queue)
                {
                    if (queue.Count == 0)
                        break;
                    callBack = queue.Dequeue();
                }
                callBack(null);
            }
            Interlocked.Increment(ref waiterCount);
        }
    }
}