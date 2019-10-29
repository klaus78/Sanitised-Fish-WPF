using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EPCISLib
{
    public class Sanitiser
    {
        const string OBJECT_EVENT = "ObjectEvent";
        const string AGGREGATION_EVENT = "AggregationEvent";
        const string TRANSACTION_EVENT = "TransactionEvent";
        const string TRANSFORMATION_EVENT = "TransformationEvent";

        const string EVENT_TIME = "eventTime";
        const string EVENT_TIMEZONE_OFFSET = "eventTimeZoneOffset";

        const string EPC_LIST = "epcList";
        const string EPC = "epc";
        const string ACTION = "action";
        const string BIZ_STEP = "bizStep";
        const string DISPOSITION = "disposition";
        const string READ_POINT = "readPoint";
        const string BIZ_LOCATION = "bizLocation";
        const string EPC_CLASS = "epcClass";

        const string PARENT_ID = "parentID";
        const string CHILD_EPCS = "childEPCs";

        const string INPUT_EPC_LIST = "inputEPCList";
        const string OUTPUT_EPC_LIST = "outputEPCList";

        const string SOURCE_LIST = "sourceList";
        const string DESTINATION_LIST = "destinationList";
        const string SOURCE = "source";
        const string DESTINATION = "destination";

        static string masking = "";

        public static void hashFile(string inputXmlFile, string outputJsonFile)
        {
            initMaskTab();

            // Loading from a file, you can also load from a stream
            var xml = XDocument.Load(inputXmlFile);

            var objectEvents = xml.Descendants(OBJECT_EVENT);
            var aggregationEvents = xml.Descendants(AGGREGATION_EVENT);
            var transactionEvents = xml.Descendants(TRANSACTION_EVENT);
            var transformationEvents = xml.Descendants(TRANSFORMATION_EVENT);

            string outputFile = "output.json";
            using (StreamWriter writer = new StreamWriter(outputFile, false))
            {
                foreach (var objectEvent in objectEvents)
                    processEvent(objectEvent, writer);

                foreach (var aggregationEvent in aggregationEvents)
                    processEvent(aggregationEvent, writer);

                foreach (var transactionEvent in transactionEvents)
                    processEvent(transactionEvent, writer);

                foreach (var transformationEvent in transformationEvents)
                    processEvent(transformationEvent, writer);
            }

        }

        //Create an array for the masking values
        static void initMaskTab()
        {
            string[,] masking_table = new string[8, 2];
            string todays = (DateTime.Now.ToString("MM/dd/yyyy"));

            masking_table[0, 0] = "AAA1";
            masking_table[1, 0] = "AAA2";
            masking_table[2, 0] = "AAB1";
            masking_table[3, 0] = "AAB2";
            masking_table[4, 0] = "AAC1";
            masking_table[5, 0] = "AAC2";
            masking_table[6, 0] = "AAD1";
            masking_table[7, 0] = "AAD2";

            masking_table[0, 1] = "10/21/2019";
            masking_table[1, 1] = "10/22/2019";
            masking_table[2, 1] = "10/23/2019";
            masking_table[3, 1] = "10/24/2019";
            masking_table[0, 1] = "10/25/2019";
            masking_table[1, 1] = "10/26/2019";
            masking_table[2, 1] = "10/27/2019";
            masking_table[3, 1] = "10/28/2019";

            for (int i = 0; i < 4; i++)
            {
                if (masking_table[i, 1] == todays)
                    masking = masking_table[i, 0];
            }

        }

        static void processEvent(XElement myEvent, StreamWriter outputFile)
        {
            // look for epc list
            var epcList = myEvent.Descendants(EPC_LIST);
            _addLine(outputFile, getListOfValues(epcList, EPC_LIST, EPC));

            // compute eventtime
            var eventTimeTag = myEvent.Descendants(EVENT_TIME);
            var eventTimeOffsetTag = myEvent.Descendants(EVENT_TIMEZONE_OFFSET);
            _addLine(outputFile, writeEventTime(EVENT_TIME, eventTimeTag, eventTimeOffsetTag));

            // look for action
            var actionTag = myEvent.Descendants(ACTION);
            _addLine(outputFile, getSingleValue(actionTag, ACTION, false, false));

            // look for bitzstep
            var bizStepTag = myEvent.Descendants(BIZ_STEP);
            _addLine(outputFile, getSingleValue(bizStepTag, BIZ_STEP, false, false));

            // look for disposition
            var dispositionTag = myEvent.Descendants(DISPOSITION);
            _addLine(outputFile, getSingleValue(dispositionTag, DISPOSITION, false, false));

            // look for readpoint
            var readPointTag = myEvent.Descendants(READ_POINT);
            _addLine(outputFile, getSingleValue(readPointTag, READ_POINT, true, true));

            // look for biz location
            var bizLocationTag = myEvent.Descendants(BIZ_LOCATION);
            _addLine(outputFile, getSingleValue(bizLocationTag, BIZ_LOCATION, true, true));

            // look for epcclass that can be contained in quantityList, childQuantityList, inputQuantityList and outputQuantityList
            var parentsEpcClass = new string[] { "quantityList", "childQuantityList", "inputQuantityList", "outputQuantityList" };
            foreach (var parent in parentsEpcClass)
            {
                var epcClassTag = myEvent.Descendants(parent);
                _addLine(outputFile, getListOfValues(epcClassTag, parent.Replace("quantityList", "epcClass")
                                                                        .Replace("QuantityList", "EpcClass"),
                                                                        EPC_CLASS));
            }

            // look for parent id
            var parentIdTag = myEvent.Descendants(PARENT_ID);
            _addLine(outputFile, getSingleValue(parentIdTag, PARENT_ID, true, false));

            // look for child epc
            var childEpcList = myEvent.Descendants(CHILD_EPCS);
            _addLine(outputFile, getListOfValues(childEpcList, CHILD_EPCS, EPC));

            // look for input epclist
            var inputEpcList = myEvent.Descendants(INPUT_EPC_LIST);
            _addLine(outputFile, getListOfValues(inputEpcList, INPUT_EPC_LIST, EPC));

            // look for output epclist
            var outputEpcList = myEvent.Descendants(OUTPUT_EPC_LIST);
            _addLine(outputFile, getListOfValues(outputEpcList, OUTPUT_EPC_LIST, EPC));

            // look for source list
            var sourceList = myEvent.Descendants(SOURCE_LIST);
            _addLine(outputFile, getListOfValues(sourceList, SOURCE_LIST, SOURCE));

            // look for destination list
            var destinationList = myEvent.Descendants(DESTINATION_LIST);
            _addLine(outputFile, getListOfValues(destinationList, DESTINATION_LIST, DESTINATION));
        }

        static string getListOfValues(IEnumerable<XElement> list, string key, string subTag)
        {
            if (list.Count() == 0)
                return "";

            StringBuilder builder = new StringBuilder();
            var epcs = list.Descendants(subTag);
            if (epcs.Count() > 0)
                builder.Append(key + "=");

            int n = 0;
            int tot = epcs.Count();
            foreach (var epc in epcs)
            {
                builder.Append(ComputeSha256Hash(epc.Value));
                if (n < (tot - 1))
                    builder.Append(",");
                n++;
            }

            return builder.ToString();
        }

        static string writeEventTime(string key, IEnumerable<XElement> eventTimeTag, IEnumerable<XElement> eventTimezoneOffset)
        {
            string timeString = getSingleTagValue(eventTimeTag);
            if (string.IsNullOrEmpty(timeString))
                return "";

            string timezoneOffsetString = getSingleTagValue(eventTimezoneOffset);

            if (!timeString.Contains('+') && !timeString.Contains('-'))
                timeString += timezoneOffsetString;

            return key + "=" + timeString;
        }

        static string getSingleValue(IEnumerable<XElement> element, string key, bool encryptionNeeded, bool maskNeeded)
        {
            string value = getSingleTagValue(element);

            if (string.IsNullOrEmpty(value))
                return "";


            if (maskNeeded)
                value = value + masking;

            if (encryptionNeeded)
                return key + "=" + ComputeSha256Hash(value);
            else
                return key + "=" + value;

        }

        static void _addLine(StreamWriter writer, string line)
        {
            if (string.IsNullOrEmpty(line))
                return;

            writer.WriteLine(line);
        }

        static string getSingleTagValue(IEnumerable<XElement> tag)
        {
            if (tag.Count() == 0 || string.IsNullOrEmpty(tag.First().Value))
                return "";

            return tag.First().Value;
        }

        public static string ComputeOnlySha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();

            }

        }

        public static string ComputeSha256Hash(string rawData)
        {
            string endText = "";
            if (rawData.Contains("sscc"))
                endText = "sscc";
            else if (rawData.Contains("sgtin"))
                endText = "sgtin";
            else if (rawData.Contains("lgtin"))
                endText = "lgtin";
            else if (rawData.Contains("gtin"))
                endText = "gtin";
            else if (rawData.Contains("sgln"))
                endText = "sgln";
            else if (rawData.Contains("pgln"))
                endText = "pgln";

            if (!string.IsNullOrEmpty(endText))
                endText = "?input=" + endText;


            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                if (!string.IsNullOrEmpty(endText))
                    builder.Append(endText);

                return builder.ToString();

            }
        }


    }
}
