using DarkRift;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DVMultiplayer.Darkrift
{
    public class BufferQueue
    {
        private readonly List<BufferItem> bufferList = new List<BufferItem>();
        public bool NotSyncedAddToBuffer(bool synced, Action<Message> action, Message message)
        {
            if (!synced)
                bufferList.Add(new BufferItem<Message>(action, message));
            return !synced;
        }

        public bool NotSyncedAddToBuffer(bool synced, Action<Message, bool> action, Message message, bool var)
        {
            if (!synced)
                bufferList.Add(new BufferItem<Message, bool>(action, message, var));
            return !synced;
        }

        public void AddToBuffer<T1, T2>(Action<T1, T2> action, T1 message, T2 var)
        {
            bufferList.Add(new BufferItem<T1, T2>(action, message, var));
        }

        public void RunBuffer()
        {
            foreach (BufferItem item in bufferList.ToList())
            {
                try
                {
                    item.RunAction();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                bufferList.Remove(item);
            }
        }

        public void RunNext()
        {
            if (bufferList.Count == 0)
                return;

            try
            {
                bufferList[bufferList.Count - 1].RunAction();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            bufferList.RemoveAt(bufferList.Count - 1);
        }
    }

    abstract public class BufferItem
    {
        public abstract void RunAction();
    }

    public class BufferItem<T1> : BufferItem
    {
        private readonly Action<T1> bufferAction;
        private readonly T1 message;

        public BufferItem(Action<T1> bufferAction, T1 message)
        {
            this.bufferAction = bufferAction;
            this.message = message;
        }

        public override void RunAction()
        {
            try
            {
                bufferAction?.Invoke(message);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }

    public class BufferItem<T1, T2> : BufferItem
    {
        private readonly Action<T1, T2> bufferAction;
        private readonly T1 message;
        private readonly T2 var;

        public BufferItem(Action<T1, T2> bufferAction, T1 message, T2 var)
        {
            this.bufferAction = bufferAction;
            this.message = message;
            this.var = var;
        }

        public override void RunAction()
        {
            try
            {
                bufferAction?.Invoke(message, var);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
