using System;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace oak_xacml.Data
{
    public class AuthzForceService
    {
        private readonly string domain;
        private readonly IHttpClientFactory _clientFactory;
        private readonly HttpClient client;
        public AuthzForceService(IConfiguration config, IHttpClientFactory clientFactory)
        {
            domain = config["authzforce_domain"];
            _clientFactory = clientFactory;
            client = clientFactory.CreateClient("AUTHZ");
        }

        public async Task<string> getCurrentRootVersion()
        {
            var data = await get("domains/"+domain+"/pap/policies/root");
            if(data != null){
                     //Console.WriteLine(raw);
                    XDocument xml = XDocument.Parse(data);
                    //Console.WriteLine(xml);
                    XElement e = GetElement(xml, "link");
                    Console.WriteLine(e.Attribute("href").Value); 
                    return e.Attribute("href").Value;
            }else{
                Console.WriteLine("No data returned from request");
                return null;
            }
        }
        public async Task<XDocument> getRootPolicyXml(string version){
            var url = "domains/"+domain+"/pap/policies/root";
            if(!string.IsNullOrEmpty(version)){
                url += "/"+version;
            }else{
                version = await getCurrentRootVersion();
                url += "/"+version;
            }
            var data = await get(url);
            if(data != null){
                XDocument xml = XDocument.Parse(data);
                
               
                return xml;
            }else{
                return null;
            }  
        }

        public XDocument CreateXML(){
            string xml = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
            <PolicySet xmlns=""urn:oasis:names:tc:xacml:3.0:core:schema:wd-17"" 
            xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
            PolicyCombiningAlgId=""urn:oasis:names:tc:xacml:3.0:policy-combining-algorithm:deny-unless-permit"" 
            PolicySetId=""root"" 
            Version=""1.0"" xsi:schemaLocation=""urn:oasis:names:tc:xacml:3.0:policy:schema:os access_control-xacml-2.0-policy-schema-os.xsd"">
            <Description> (generated) </Description>
            <Target/>
            </PolicySet>";
            XDocument doc = XDocument.Parse(xml);
            return doc;

        }

        public async Task<string> updateRootPolicy(XDocument d)
        {
            
            if(d.Root.Name == "{urn:oasis:names:tc:xacml:3.0:core:schema:wd-17}Policy"){
                var desc = "(generated)";
                var el = GetElement(d, "Description");
                if(el != null){
                    desc = el.Value;
                }
                var policySet = CreateXML();
                policySet.Root.SetElementValue("{urn:oasis:names:tc:xacml:3.0:core:schema:wd-17}Description", desc);
                
                var policyAttributes = d.Root.Attributes();
                //d.Root.RemoveAttributes();
                foreach(var att in policyAttributes){
                    Console.WriteLine("attname: "+ att.Name);
                    if(att.Name == "PolicyId"){
                        d.Root.SetAttributeValue(att.Name, att.Value);
                    }
                    if(att.Name == "RuleCombiningAlgorithm"){
                        d.Root.SetAttributeValue(att.Name, att.Value);
                    }
                    if(att.Name == "Version"){
                        d.Root.SetAttributeValue(att.Name, att.Value);
                    }
                }
                policySet.Root.Add(d.Root);
                d = policySet;          
                //Console.WriteLine(d);    
            }
           
            //Set PolicySetId to root
            d.Root.Attribute("PolicySetId").SetValue("root");
            
            //Get current root policy version and increment and set version. 
            var version = await getCurrentRootVersion();
            var vSize = version.Split(".");
            int lastInt = Int32.Parse(vSize[vSize.Length-1]);
            lastInt += 1;
            vSize[vSize.Length-1] = lastInt.ToString();
            version = String.Join(".", vSize);
            d.Root.Attribute("Version").SetValue(version);

            //Console.WriteLine(d);

            var data = await post("domains/"+domain+"/pap/policies", d);     
            if(data != null){
                //XDocument res = XDocument.Parse(data);
                //Console.WriteLine(res);
                return data;
            }else{
                return null;
            }
        }
        public async Task<List<LogEntry>> listRootVersionHistory()
        {
            List<LogEntry> list = new List<LogEntry>(); 
           
            var data = await get("domains/"+domain+"/pap/policies/root");
            if(data != null){ 
                XDocument xml = XDocument.Parse(data);
                foreach (XNode node in xml.DescendantNodes())
                {
                    if (node is XElement)
                    {
                        XElement element = (XElement)node;
                        if (element.Name.LocalName.Equals("link")){
                            list.Add( new LogEntry(){
                                Version = element.Attribute("href").Value,
                                Description = "",
                                TimeStamp = "",
                                requestFile = "",
                                decision = ""
                            });
                        }
                            
                    }
                }

                if(list.Count > 0){
                    foreach(var l in list){
                        var pol = await getRootPolicyXml(l.Version);
                        if(pol != null){
                            var el = GetElement(pol, "Description");
                            if(el != null){
                                l.Description = el.Value;
                            }else{
                                l.Description = "(empty)";
                            }
                            
                        }
                    }
                    return list;
                }else{
                    return null;
                }


                    
            }else{
                return null;
            }
           
        }
        public async void listPolicies(){
            var req = new HttpRequestMessage(HttpMethod.Get, "domains/"+domain+"/pap/policies");
            var res = await client.SendAsync(req);
            if(res.IsSuccessStatusCode)
            {
                var raw = await res.Content.ReadAsStringAsync();
                if(raw.Length > 0)
                {
                    //Console.WriteLine(raw);
                    XDocument xml = XDocument.Parse(raw);
                    Console.WriteLine(xml);
                    XElement e = GetElement(xml, "link");
                    Console.WriteLine(e.LastAttribute);              
                }
                
            }
        }

        public async Task<string> sendDescisionRequest(XDocument r){
            var data = await post("domains/"+domain+"/pdp", r);
            if(data != null){
                var xml = XDocument.Parse(data);
                var el = GetElement(xml, "Decision");
                if(el != null){
                    Console.WriteLine(el.Value);
                    return el.Value;
                }else{
                    return data;
                }
                
            }else{
                return null;
            }
        }

        public async Task<string> get(string url){
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("ContentType", "application/xml");
            var res = await client.SendAsync(req);
            if(res.IsSuccessStatusCode)
            {
                var raw = await res.Content.ReadAsStringAsync();
                if(raw.Length > 0)
                {
                    return raw;

                }else{
                    return null;
                }
            }else{
                return null;
            }
        }
        public async Task<string> post(string url, XDocument xml){
            var req = new HttpRequestMessage(HttpMethod.Post, url);
            var str = xml.ToString();
            req.Content = new StringContent(str, Encoding.UTF8, "application/xml");
            
            var res = await client.SendAsync(req);
            if(res.IsSuccessStatusCode)
            {
                var raw = await res.Content.ReadAsStringAsync();
                if(raw.Length > 0)
                {
                    //Console.WriteLine(raw);
                    return raw;
                    

                }else{
                    return null;
                }
            }else{
                
                //Console.WriteLine(res.ToString());
                //Console.WriteLine(await res.Content.ReadAsStringAsync());
                return await res.Content.ReadAsStringAsync();
            }
        }
        private XElement GetElement(XDocument doc,string elementName)
        {
            foreach (XNode node in doc.DescendantNodes())
            {
                if (node is XElement)
                {
                    XElement element = (XElement)node;
                    if (element.Name.LocalName.Equals(elementName))
                        return element;
                }
            }
            return null;
        }

    }
}