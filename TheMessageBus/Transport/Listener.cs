using TheMessageBus.Messages;

namespace TheMessageBus
{
    public class Listener
    {
        public delegate void MessageReceivedEventHandler(object Sender, SubjectInfo Subject, ApplicationInfo Application, Message Data);

        public event MessageReceivedEventHandler MessageReceived;

        private Transport Transport;
        private string Subject;

        public Listener(Transport Transport, string Subject)
        {
            this.Transport = Transport;
            this.Subject = Subject;
            Transport.Subscribe(Subject);
            Transport.ReliableMessage += Transport_ReliableMessage;
        }

        private void Transport_ReliableMessage(object Sender, SubjectInfo Subject, ApplicationInfo Application, byte[] Data)
        {
            if (CheckSubject(this.Subject, Subject.SendSubject))
                MessageReceived?.Invoke(this, Subject, Application, new Message(Data));
        }

        private bool CheckSubject(string s1, string s2)
        {
            string[] n1 = s1.Split('.');
            string[] n2 = s2.Split('.');
            for (int i = 0; i < n1.Length; i++)
            {
                if (i >= n2.Length) return false;
                if (n1[i][0] == '>') return true;
                if (n1[i][0] != '*' && n1[i] != n2[i]) return false;
            }
            return true;
        }

        public void Destroy()
        {
            Transport.Unsubscribe(Subject);
        }
    }
}