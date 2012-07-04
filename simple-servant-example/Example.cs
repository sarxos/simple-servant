using System;
using System.Net;
using System.Threading;
using SarXos.SimpleServant;


namespace Example {

    public class Program {
        static void Main(string[] args) {
            Servant servant = new Servant(6756); // invoke localhost/Close to stop app
            servant.register(new Test());
            servant.register(new Test2());
            while (servant.Running) {
                Thread.Sleep(100);
            }
        }
    }

    public class TestObject {
        public string name = "Bobek";
        public string location = "Dolina Muminkow";
        public int age = 21;
    }

    public class Test : Command {
        public override object Run(HttpListenerRequest request, HttpListenerResponse response) {
            return new TestObject();
        }
    }
    public class Test2 : Command {
        public override object Run(HttpListenerRequest request, HttpListenerResponse response) {
            return "abcd";
        }
    }
}
