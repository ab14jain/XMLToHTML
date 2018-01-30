using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Configuration;
using Microsoft.SharePoint.Client;

namespace XMLToHTML
{
    class Program
    {
        
        static void Main(string[] args)
        {
            var appSettings = ConfigurationManager.AppSettings;
            string source = appSettings["SourceLocation"];
            
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(source + "frmSetup.xml");            
            XmlNodeList nodeList = xmlDoc.SelectNodes("/portalMigratorData/job/data/records/record");
            string leftNavHtml = "<ul>";

            foreach (XmlNode node in nodeList)
            {
                leftNavHtml += LeftNavigation(node.ChildNodes);
            }

            leftNavHtml += "<ul/>";

            foreach (XmlNode node in nodeList)
            {
                string unid = null;
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    if (childNode.Attributes["name"].Value == "unid")
                    {
                        unid = childNode.InnerText;
                    }
                    if (childNode.Attributes["name"].Value == "RenderHtml") {
                        childNode.InnerText = childNode.InnerText.Replace("~parts/", "");
                        var folderPath = CreateFolder(unid);
                        CreateHTML(childNode.InnerText, unid, folderPath, leftNavHtml);
                    }
                    //resultHTML += "<div class='fieldName'> Field Name : " + childNode.Attributes["name"].Value + "</div> <div class='class='fieldValue'> Value : " + childNode.InnerText + "</div>";
                }                
            }
            Console.WriteLine("Created Successfully!!");
            Console.ReadLine();
        }

        public static string LeftNavigation(XmlNodeList nodeList)
        {
            string leftNavHTML = "";
            string unid = "";
            var appSettings = ConfigurationManager.AppSettings;
            string detination = appSettings["DestinationLocation"];

            foreach (XmlNode childNode in nodeList)
            {
                if (childNode.Attributes["name"].Value == "unid")
                {
                    unid = childNode.InnerText;
                    leftNavHTML += "<li style='padding: 5px;'><a href='" + detination + unid + "\\" + unid + ".html'>" + childNode.InnerText + "</a></li>";
                    break;
                }
            }
            
            return leftNavHTML;
        }
        public static void CreateHTML(string htmlBodyContent, string unid, string folderPath, string leftNavHtml)
        {

            using (FileStream fileStream = new FileStream(folderPath + (unid + ".html"), FileMode.Create))
            {
                using (StreamWriter streamWriter = new StreamWriter(fileStream, Encoding.Unicode))
                {
                    streamWriter.WriteLine("<html>");
                    streamWriter.WriteLine("<head>");
                    streamWriter.WriteLine("<title>ASM</title>");
                    streamWriter.WriteLine("</head>");
                    streamWriter.WriteLine("<body bgcolor=\"#ffffff\">");
                    streamWriter.WriteLine("<div style='float:left;width:20%;height:auto;'>" + leftNavHtml + "</div>");                    
                    streamWriter.WriteLine("<div style='float:left;width:78%;height:auto;text-align:center;'><h1>Autoliv Supplier Manual</h1></center>");
                    streamWriter.WriteLine("<center>");
                    streamWriter.WriteLine("<p>");
                    streamWriter.WriteLine(htmlBodyContent);
                    streamWriter.WriteLine("</p>");
                    streamWriter.WriteLine("</div>");
                    streamWriter.WriteLine("</body>");
                    streamWriter.WriteLine("</html>");

                    //streamWriter.WriteLine(@"<!DOCTYPE html> <html lang='en' xmlns='http://www.w3.org/1999/xhtml'>");
                    //streamWriter.WriteLine(@"<head><style>#header{height:125px;width:100%;background-color:aqua;font-weight:500;text-align:center;vertical - align : middle;                    line - height:100px;}        .main - container{                height: 600px;                width: 100 %;                }                # left-navigation{            height: 200px;            width: 20 %;                background - color: green;                float:left;            }# content {        height: auto;        width: 80 %;            background - color: yellow;            float:left;        }  .fieldValue{            padding: 10px;                    background - color: #eee;        }          .fieldName{                padding: 10px;                    background - color: #808080;        }  </style></head>");
                    //streamWriter.WriteLine(@"<body><div id='header' class='header'><h1> ASM </h1></div><div id = 'mainCcontainer' class='main-container'><div id = 'left-navigation'></div><div id='content'>");
                    //streamWriter.WriteLine(htmlBodyContent);
                    //streamWriter.WriteLine(@"</div></div></body>");
                    //streamWriter.WriteLine(@"</html>");
                }

            }
        }

        public static string CreateFolder(string folderName)
        {
            var appSettings = ConfigurationManager.AppSettings;
            string detination = appSettings["DestinationLocation"];
            string path = detination + folderName + "\\";
            if (!Directory.Exists(path))  
            {  
               Directory.CreateDirectory(path);
            }
            return path;
        }

        private string UploadToSharePoint(byte[] convertedByteArray, Stream reqData, string baseFileName, string watermarkedLibrary)
        {
            string siteUrl = "";  //reqData.SiteAbsoluteUrl;
            //Insert Credentials
            ClientContext context = new ClientContext(siteUrl);

            //SecureString passWord = new SecureString();
            //foreach (var c in "mypassword") passWord.AppendChar(c);
            //context.Credentials = new SharePointOnlineCredentials("myUserName", passWord);

            Web site = context.Web;

            string newFileName = baseFileName.Split('.')[0] + DateTime.Now.ToString("yyyyMMdd") + "." + baseFileName.Split('.')[1];


            //System.IO.File.WriteAllBytes("Foo.txt", convertedByteArray);

            FileCreationInformation newFile = new FileCreationInformation();

            newFile.ContentStream = new MemoryStream(convertedByteArray); //convertedByteArray;// System.IO.File.WriteAllBytes("", convertedByteArray);
            newFile.Url = System.IO.Path.GetFileName(newFileName);
            newFile.Overwrite = true;

            Microsoft.SharePoint.Client.List docs = site.Lists.GetByTitle(watermarkedLibrary);
            Microsoft.SharePoint.Client.File uploadFile = docs.RootFolder.Files.Add(newFile);

            context.Load(uploadFile);

            context.ExecuteQuery();

            //Return the URL of the new uploaded file
            string convertedFileLocation = ""; // reqData.WebServerRelativeUrl + "/" + watermarkedLibrary + "/" + newFileName;

            return convertedFileLocation;
        }

    }
}
