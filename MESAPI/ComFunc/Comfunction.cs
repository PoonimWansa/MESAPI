using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace MESAPI.ComFunc
{
    public class Comfunction
    {


        public static string connection1()
        {

            string connection1 = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)"
            + "(HOST = THBPODSMRACDB)(PORT = 1521))"
            + "(CONNECT_DATA = (SERVER = DEDICATED)"
            + "(SERVICE_NAME = THSFDB)));Password=Pass1234;User ID=DET_AM";
            return connection1;
        }


        public static string connection3()
        {

            string connection3 = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)"
           + "(HOST = THBPODSMRACDB)(PORT = 1521))"
           + "(CONNECT_DATA = (SERVER = DEDICATED)"
           + "(SERVICE_NAME = THSFDB)));Password=SFISM4;User ID=SFISM4";
            return connection3;
        }
        public static string connection4()
        {
            string connection4 = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)"
        + "(HOST = THBPODSMRACDB)(PORT = 1521))"
        + "(CONNECT_DATA = (SERVER = DEDICATED)"
        + "(SERVICE_NAME = THSFDB)));Password=detmp2007man;User ID=DET_MP";
            return connection4;

        }
        public static string connection5()
        {
            string connection5 = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)"
        + "(HOST = THBPODSMRACDB)(PORT = 1521))"
        + "(CONNECT_DATA = (SERVER = DEDICATED)"
        + "(SERVICE_NAME = THSFDB)));Password=dethp2007man;User ID=DET_HP";
            return connection5;

        }
        public static string connection6()
        {
            string connection6 = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)"
        + "(HOST = THBPODSMRACDB)(PORT = 1521))"
        + "(CONNECT_DATA = (SERVER = DEDICATED)"
        + "(SERVICE_NAME = THSFDB)));Password=detfm2007man;User ID=DET_FM";
            return connection6;

        }
        public static string connection7()
        {
            string connection6 = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)"
        + "(HOST = THBPODSMRACDB)(PORT = 1521))"
        + "(CONNECT_DATA = (SERVER = DEDICATED)"
        + "(SERVICE_NAME = THSFDB)));Password=detcndc2007man;User ID=DET_CNDC";
            return connection6;

        }
        public static string connectionMS()
        {
            string connectionMS = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)"
        + "(HOST = THBPODSMRACDB)(PORT = 1521))"
        + "(CONNECT_DATA = (SERVER = DEDICATED)"
        + "(SERVICE_NAME = THSFDB)));Password=detms2007man;User ID=DET_MS";
            return connectionMS;

        }
    }
}