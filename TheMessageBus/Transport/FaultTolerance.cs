using System;

namespace TheMessageBus
{
    public class FaultTolerance
    {
        public delegate void FaultToleranceChangedEventHandler(object Sender, bool Activation);

        public event FaultToleranceChangedEventHandler FaultToleranceChanged;

        private double HeartbeatInterval;
        private object p;
        private Transport transport;
        private string GroupName;
        private string GetUniqueId;
        private System.Timers.Timer heartbeatHeartBeatTimer = null;
        private Listener Listener;
        private bool ActiveFlag = false;
        private DateTime LastHeartBeat = DateTime.MaxValue;

        public FaultTolerance(Transport Transport, string GroupName, double HeartbeatInterval, object p = null)
        {
            this.transport = Transport;
            this.GroupName = GroupName;
            this.HeartbeatInterval = HeartbeatInterval;
            this.p = p;

            Transport.Disconnected += Transport_Disconnected;
            Transport.Connected += Transport_Connected;

            LastHeartBeat = DateTime.Now;
            GetUniqueId = Transport.GetUniqueId();

            Listener = new Listener(Transport, "$FT.*." + GroupName);
            Listener.MessageReceived += L_MessageReceived;

            heartbeatHeartBeatTimer = new System.Timers.Timer();
            heartbeatHeartBeatTimer.Interval = TimeSpan.FromSeconds(this.HeartbeatInterval).TotalMilliseconds;
            heartbeatHeartBeatTimer.Elapsed += HeartbeatHeartBeatTimer_Elapsed;
            StartHB();
        }

        private void StartHB()
        {
            SetFlag(false);
            LastHeartBeat = DateTime.Now;
            heartbeatHeartBeatTimer.Start();
        }

        private void StopHB()
        {
            SetFlag(false);
            LastHeartBeat = DateTime.MaxValue;
            heartbeatHeartBeatTimer.Stop();
        }

        private void Transport_Connected(object sender)
        {
            StartHB();
        }

        private void Transport_Disconnected(object sender)
        {
            StopHB();
        }

        // https://en.wikipedia.org/wiki/Bully_algorithm
        private void L_MessageReceived(object Sender, TheMessageBus.Messages.SubjectInfo Subject, TheMessageBus.Messages.ApplicationInfo Application, Message Data)
        {
            switch (Subject.SendSubject.Split('.')[1])
            {
                case "ELECTION":
                    var uu = Data.Get<TheMessageBus.Messages.FaultTolerance.Election>();
                    if (GetUniqueId.CompareTo(uu.ClientId) == 1)
                        transport.SendReply(Subject, new TheMessageBus.Messages.FaultTolerance.Election { ClientId = GetUniqueId });
                    break;

                case "HB":
                    LastHeartBeat = DateTime.Now;
                    break;

                default:
                    break;
            }
        }

        private void HeartbeatHeartBeatTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (ActiveFlag)
            {
                LastHeartBeat = DateTime.Now;
                transport.Send("$FT.HB." + GroupName, new Messages.FaultTolerance.Heartbeat { ClientId = GetUniqueId });
                return;
            }

            if (DateTime.Now.Subtract(LastHeartBeat).TotalSeconds > HeartbeatInterval)
            {
                // Master not available again - use Bully to elect the Master
                var n = transport.SendRequest("$FT.ELECTION." + GroupName, new Messages.FaultTolerance.Election { ClientId = GetUniqueId }, (int)HeartbeatInterval);
                SetFlag(n == null ? true : false);
            }
        }

        private void SetFlag(bool flag)
        {
            if (flag == ActiveFlag) return;
            ActiveFlag = flag;
            FaultToleranceChanged?.Invoke(p, flag);
        }

        public void Destroy()
        {
            Listener?.Destroy();
        }
    }
}