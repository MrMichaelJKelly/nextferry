﻿using System;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace WCFServiceWebRole1
{
    public class Service1 : IService1
    {

        public string GetSchedules(string clientversion, string lastdataversion)
        {
            DateTime dv;
            if (DateTime.TryParse(lastdataversion, out dv) &&
                dv <= currentexpiration)
            {
                return "";
            }
            else
            {
                return currentschedule;
            }
        }

        public string GetDelays(string clientversion, string location)
        {
            return "";
        }

        public string GetAlerts(string clientversion)
        {
            return "";
        }

        DateTime currentexpiration = new DateTime(2012, 12, 29);
        string currentschedule =
@"bainbridge,wd,330,370,425,475,525,575,640,685,740,790,845,900,945,1000,1050,1100,1160,1210,1260,1325,1375,1455,1535
bainbridge,we,370,475,525,575,635,685,740,790,845,900,945,1000,1050,1100,1160,1210,1260,1305,1360,1395,1485,1570
bainbridge,ed,285,320,380,425,475,525,580,625,690,740,790,845,895,950,995,1050,1110,1150,1210,1255,1305,1415,1495
bainbridge,ee,320,425,525,575,625,690,740,790,845,895,950,995,1050,1110,1150,1210,1255,1305,1350,1440,1525
edmonds,wd,335,380,430,475,530,580,630,670,725,760,820,865,915,955,1005,1045,1095,1140,1180,1230,1260,1345,1425
edmonds,we,430,470,530,580,630,670,725,760,820,865,915,955,1005,1045,1095,1140,1180,1230,1280,1335,1365,1425
edmonds,ed,295,335,385,425,475,520,580,625,675,715,770,810,870,910,960,1000,1050,1090,1140,1185,1220,1300,1385
edmonds,ee,385,425,475,520,580,625,675,715,770,810,870,910,960,1000,1050,1090,1140,1185,1225,1270,1320,1385
mukilteo,wd,305,335,360,390,420,450,480,510,540,570,600,630,660,690,720,750,780,810,840,870,900,930,960,990,1020,1050,1080,1110,1140,1175,1200,1225,1260,1320,1380,1440,1505
mukilteo,we,360,420,480,510,540,570,600,630,660,690,720,750,780,810,840,870,900,930,960,990,1020,1050,1080,1110,1140,1175,1200,1225,1260,1285,1320,1380,1440,1505
mukilteo,ed,280,310,330,360,390,420,450,480,510,540,570,600,630,660,690,720,750,780,810,840,870,900,930,960,990,1020,1050,1080,1110,1140,1170,1200,1230,1290,1350,1410,1470
mukilteo,ee,330,390,450,480,510,540,570,600,630,660,690,720,750,780,810,840,870,900,930,960,990,1020,1050,1080,1110,1140,1170,1200,1230,1255,1290,1350,1410,1470
pt townsend,wd,435,525,615,705,795,900,990,1080,1170,1275
pt townsend,we,435,525,615,705,795,900,990,1080,1170,1275
pt townsend,ed,390,480,570,660,750,855,945,1035,1125,1230
pt townsend,ee,390,480,570,660,750,855,945,1035,1125,1230
fauntleroy-southworth,wd,265,315,350,425,465,525,545,565,620,660,700,740,820,860,905,935,980,1020,1060,1110,1175,1255,1340,1420,1495,1570
fauntleroy-southworth,we,315,365,415,455,515,555,610,645,705,740,820,860,900,960,1000,1080,1100,1180,1340,1420,1495,1570
fauntleroy-southworth,ed,265,300,360,400,475,500,560,590,610,670,695,750,790,870,895,950,965,1025,1070,1110,1160,1230,1385,1465,1540
fauntleroy-southworth,ee,265,365,410,460,500,560,600,655,690,750,790,865,910,950,1010,1050,1115,1150,1230,1385,1465,1540
fauntleroy-vashon,wd,315,350,370,405,425,505,545,565,620,640,700,715,740,775,800,820,885,905,960,1000,1020,1060,1080,1110,1145,1175,1205,1255,1280,1340,1420,1495,1570
fauntleroy-vashon,we,315,365,415,455,515,555,610,645,705,740,770,800,820,840,860,900,920,960,980,1000,1030,1060,1100,1120,1160,1180,1200,1280,1340,1420,1495,1570
fauntleroy-vashon,ed,245,285,320,345,380,400,420,435,475,495,520,540,580,610,630,690,715,745,770,830,860,925,990,1030,1050,1110,1135,1180,1230,1250,1315,1365,1445,1520
fauntleroy-vashon,ee,285,335,385,430,480,520,580,620,675,710,740,770,795,810,830,870,885,930,950,970,1005,1030,1055,1070,1090,1135,1150,1170,1250,1315,1365,1445,1520
vashon-southworth,wd,245,340,375,450,570,590,645,725,765,845,930,1045,1085,1140,1200,1280,1365,1445,1520,1600
vashon-southworth,we,340,390,440,480,540,580,635,670,730,770,845,885,925,985,1025,1125,1205,1365,1445,1520,1600
vashon-southworth,ed,265,300,360,400,475,500,560,590,610,670,695,750,790,870,965,1025,1110,1160,1230,1385,1465,1540
vashon-southworth,ee,265,365,410,460,500,560,600,655,690,750,790,865,910,950,1010,1050,1115,1150,1230,1385,1465,1540
bremerton,wd,360,455,510,600,670,755,810,900,980,1055,1125,1195,1265,1350,1490
bremerton,we,360,455,510,600,670,755,810,900,980,1055,1125,1195,1265,1350,1490
bremerton,ed,290,380,440,525,585,670,740,825,900,980,1050,1125,1195,1265,1420
bremerton,ee,380,440,525,585,670,740,825,900,980,1050,1125,1195,1265,1420
pt defiance,wd,330,380,430,480,530,580,630,680,730,850,910,965,1020,1080,1140,1200,1260,1320,1375
pt defiance,we,380,430,480,530,580,630,680,730,780,850,910,965,1020,1080,1140,1200,1260,1320,1375
pt defiance,ed,305,355,405,455,505,555,605,655,705,820,880,940,995,1050,1110,1165,1225,1290,1350
pt defiance,ee,355,405,455,505,555,605,655,705,755,820,880,940,995,1050,1110,1165,1225,1290,1350
friday harbor,wd,260,380,510,570,880,990,1080,1225
friday harbor,we,260,380,510,570,880,990,1080,1225
friday harbor,ed,350,485,665,835,975,1105,1185,1325
friday harbor,ee,350,485,665,835,855,975,1105,1185,1325
orcas,wd,330,455,635,935,1105,1255
orcas,we,330,455,635,835,935,1105,1255
orcas,ed,405,530,730,1030,1185,1325
orcas,ee,405,530,730,915,1030,1185,1325";


    }
}