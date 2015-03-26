using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;

namespace MilitaryPlanner.Helpers
{
    public class BaseMessage<T> : MessageBase where T : class
    {
        public BaseMessage()
        {
            
        }

        public BaseMessage(object sender, object target) : base(sender, target)
        {
            
        }

        public static void Register(object recipient, Action<T> action)
        {
            Messenger.Default.Register(recipient, action);
        }

        public static void Unregister(object recipient)
        {
            Messenger.Default.Unregister<T>(recipient);
        }

        public void SendAfterWaiting(int milliseconds)
        {
            InternalSend(milliseconds);
        }

        public virtual void Send()
        {
            InternalSend();
        }

        protected void InternalSend(int delay = 0)
        {
            if (delay > 0)
            {
                new Thread(() =>
                {
                    Thread.Sleep(delay);
                    InternalSendOnUIThread();
                }){IsBackground = true}.Start();
            }
            else
            {
                InternalSendOnUIThread();
            }
        }

        private void InternalSendOnUIThread()
        {
            
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                Messenger.Default.Send(this as T);
            });
            
        }
    }
}
