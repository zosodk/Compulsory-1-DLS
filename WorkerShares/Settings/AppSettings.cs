using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerShares.Settings
{
    public class AppSettings
    {
        public RabbitMQSettings RabbitMQ { get; set; }
        public OtlpSettings Otlp { get; set; }
        public MaildirSettings Maildir { get; set; }
        public SeqSettings Seq { get; set; }
        public DatabaseSettings Database { get; set; }
    }
}
