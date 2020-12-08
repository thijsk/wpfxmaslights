using System;
using System.Threading;

namespace xmaslights
{
    public static class SingleInstance
    {
        private static Mutex m_Mutex;

        public static bool IsFirstInstance(string UniqueIdentifier)
        {
            if (string.IsNullOrWhiteSpace(UniqueIdentifier))
                throw new ArgumentNullException("UniqueIdentifier");

            m_Mutex = new Mutex(true, UniqueIdentifier);
            if (m_Mutex.WaitOne(1, true))
            {
                return true;
            }
            else
            {
                Close();
                return false;
            }
        }

        public static void Close()
        {
            if (m_Mutex != null)
            {
                m_Mutex.Close();
                m_Mutex = null;
            }
        }
    }
}
