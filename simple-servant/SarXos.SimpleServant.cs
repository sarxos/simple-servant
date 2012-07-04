using System;
using System.Net;
using System.IO;
using System.Threading;
using System.Xml.Serialization;
using System.Xml;
using System.Text;
using System.Collections.Generic;


namespace SarXos.SimpleServant {

    public class Servant {

        private Dictionary<String, Command> commands = new Dictionary<String, Command>();
        private Thread thread;
        private HttpListener listener;
        private StreamWriter logger;
        private String address;

        private bool running = true;
        public bool Running {
            get {
                return running;
            }
        }

        public Servant(int port) {
            this.logger = new StreamWriter("servant.log", true);
            this.logger.AutoFlush = true;
            this.address = "http://127.0.0.1:" + port + "/";
            this.init();
        }

        public void register(Command command) {
            String name = command.GetType().ToString();
            log("register {0}", name);
            commands.Add(name, command);
        }

        private void log(String message, params object[] args) {
            String date = String.Format("[{0:HH:mm:ss}]", DateTime.Now);
            logger.WriteLine(date + " " + message, args);
        }

        private void init() {

            log("init");

            // setup listener
            listener = new HttpListener();
            listener.Prefixes.Add(address);

            // setup thread
            thread = new Thread(Worker);
            thread.IsBackground = true;
            thread.Priority = ThreadPriority.Normal;
            thread.Start(null);
        }

        private void Worker(object state) {
            
            // start listening
            listener.Start();

            log("running");

            // request -> response loop
            while (true) {

                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;

                log("incoming request");

                using (HttpListenerResponse response = context.Response) {

                    String[] parts = request.Url.PathAndQuery.Split('/');
                    String[] args = new String[parts.Length - 2];

                    for (int i = 2; i < parts.Length; i++) {
                        args[i - 2] = parts[i];
                    }

                    String action = parts[1];

                    if ("Close".Equals(action)) {
                        log("turning servant off");
                        running = false;
                        break;
                    }

                    Type[] types = new Type[] {
                        typeof(String), 
                        typeof(String[]) 
                    };
                    Object[] values = new Object[] { 
                        action, 
                        args 
                    };

                    log("executing action {0}", action);

                    String type = action.Replace('/', '.');
                    Command command = null;
                    foreach (KeyValuePair<String, Command> entry in commands) {
                        if (entry.Key.Equals(type)) {
                            command = entry.Value;
                            break;
                        }
                    }

                    if (command == null) {
                        log("cannot find type {0}", type);
                        continue;
                    }

                    Object artifact = command.Run(request, response);
                    if (artifact == null) {
                        log("returned artifact should not be null!");
                        continue;
                    }

                    XmlSerializer serializer = new XmlSerializer(artifact.GetType()); // TODO: tune up by caching
                    StringWriter sw = new StringWriter();
                    XmlWriter writer = XmlWriter.Create(sw);
                    serializer.Serialize(writer, artifact);
                    String xml = sw.ToString();

                    byte[] data = Encoding.UTF8.GetBytes(xml);
                    response.ContentType = "application/xml";
                    response.ContentLength64 = data.Length;

                    using (Stream output = response.OutputStream) {
                        output.Write(data, 0, data.Length);
                    }
                }

                log("request handled");
            }

            log("stop");

            dispose();
        }

        private void dispose() {
            logger.Close();
        }
    }

    public abstract class Command {

        public String Action {
            get;
            set;
        }

        public String[] Arguments {
            get;
            set;
        }

        public abstract Object Run(HttpListenerRequest request, HttpListenerResponse response);

    }
}
