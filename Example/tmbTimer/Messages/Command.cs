using System;

namespace Messages
{
    public class Command
    {
        public class GetTime
        {
            public class Request : Base.Request
            {
                public Request()
                {
                    Command = "GetTime";
                }
            }

            public class Response : Base.Response
            {
                public DateTime Now { get; set; }
            }
        }

        public class Base
        {
            public class Request
            {
                public string Command { get; set; }
            }

            public class Response
            {
                public int ErrorCode { get; set; } = 0;
                public string ErrorText { get; set; } = "success";
            }
        }
    }
}