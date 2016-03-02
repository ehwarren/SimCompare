using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SimCompare
{
    class FileMan
    {
        public String[] originalFile;
        public String[] fileList;
        public String[] originalFileToParse;
        public String[] modifiedFilesToParse;

        public FileMan()
        {
            refreshFiles();
        }

        public void refreshFiles()
        {
            try
            {
                originalFile = Directory.GetFiles("./original/", "*.xml").OrderByDescending(d => new FileInfo(d).CreationTime).ToArray();
            }
            catch
            {
                Console.WriteLine("Could not open original xml file...");
            }
            try
            {
                fileList = Directory.GetFiles("./changes/", "*.xml").OrderByDescending(d => new FileInfo(d).CreationTime).ToArray();
            }
            catch
            {
                Console.WriteLine("Could not open changed xml file...");
            }
        }

        public void parseFilesInOne()
        {
            //Load the XML data for the original file
            XDocument orig = XDocument.Load(originalFileToParse[0]);
            //Load the xml data for each of the simulations
            XDocument[] changes = new XDocument[modifiedFilesToParse.Length];
            for (int i = 0; i < modifiedFilesToParse.Length; i++)
            {
                changes[i] = XDocument.Load(modifiedFilesToParse[i]);
            }

            //This list is used to store the values we grab from the XML files. 
            //Each of the list values will eventuall be one line in our CSV file.
            List<String> values = new List<String>();

            String currentLine = "";
            //Add the tags for each of the simulations
            currentLine = "ORIGINAL DATA,,,,,,,";
            for (int i = 0; i < modifiedFilesToParse.Length; i++)
            {
                currentLine += "SIMULATION ";
                currentLine += i + 1;
                currentLine += ",,,,,,,,";
            }
            values.Add(currentLine);
            //add the column headers for each of the simulations
            currentLine = "shapeId, lugIndex, name, grade, price, volume,,";
            for (int i = 0; i < modifiedFilesToParse.Length; i++)
            {
                currentLine += "shapeId, lugIndex, name, grade, price, volume,isDifferent,,";
            }
            values.Add(currentLine);
            //clear our currentLine variable because we've added it to the list already
            currentLine = "";

            //Loop through each of the files we want to compare the original for..
            Console.WriteLine("Parsing the files...");
            //Loop through all of the XML data..
            for (int j = 0; j < orig.Root.Elements().Count(); j++)
            {
                //Flag to see if the original data(only checking name), is differnet from the other file
                bool isDifferent = false;

                //Create our xelement for this section of original, and modified data
                XElement orig_data = orig.Root.Elements().ElementAt(j);

                //Declare an empty string to be used as a placeholder for data before we add it to the String List values.
                string entry = "";

                //Loop through each element in that section of of the XML file. Add the original data, then the changed data, and then compare
                for (int i = 0; i < orig_data.Elements().Count(); i++)
                {
                    try
                    {
                        //we only keep shapeId, lugIndex, name, grade, price and volume
                        if (orig_data.Elements().ElementAt(i).Name.LocalName == "shapeId" || orig_data.Elements().ElementAt(i).Name.LocalName == "lugIndex" || orig_data.Elements().ElementAt(i).Name.LocalName == "name" || orig_data.Elements().ElementAt(i).Name.LocalName == "grade" || orig_data.Elements().ElementAt(i).Name.LocalName == "price")
                        {
                            //This is to account for a no solution 
                            if (orig_data.Elements().ElementAt(i).Name.LocalName == "price" && orig_data.Elements().ElementAt(i).Value == "0.0")
                                entry += ",,";
                            entry += (orig_data.Elements().ElementAt(i).Value + ",");
                        }
                        else if (orig_data.Elements().ElementAt(i).Name.LocalName == "volume")
                        {
                            entry += (orig_data.Elements().ElementAt(i).Value + ",,");

                        }
                        //values.Add(el.Attribute("Value").Value.ToString());
                    }
                    catch { }
                }

                //now we loop through each of our changed files and add the entry to the list
                for (int sim = 0; sim < changes.Length; sim++)
                {
                    isDifferent = false;    //BugFix1 - Didn't clear the variable, so if the first file compared was different it would assume all the rest were too...
                    XElement element = changes[sim].Root.Elements().ElementAt(j);
                    for (int i = 0; i < element.Elements().Count(); i++)
                    {
                        try
                        {
                            //we only keep shapeId, lugIndex, name, grade, price and volume
                            if (element.Elements().ElementAt(i).Name.LocalName == "shapeId" || element.Elements().ElementAt(i).Name.LocalName == "lugIndex" || element.Elements().ElementAt(i).Name.LocalName == "name" || element.Elements().ElementAt(i).Name.LocalName == "grade" || element.Elements().ElementAt(i).Name.LocalName == "price")
                            {
                                if (element.Elements().ElementAt(i).Name.LocalName == "price" && orig_data.Elements().ElementAt(i).Value == "0.0")
                                    entry += ",,";
                                entry += (element.Elements().ElementAt(i).Value + ",");
                            }
                            else if (element.Elements().ElementAt(i).Name.LocalName == "volume")
                            {
                                entry += (element.Elements().ElementAt(i).Value);

                            }
                            if (element.Elements().ElementAt(i).Name.LocalName == "name")
                            {
                                if (element.Elements().ElementAt(i).Value != orig_data.Elements().ElementAt(i).Value)
                                    isDifferent = true;
                            }
                        }
                        catch { }
                    }
                    if (isDifferent)
                        entry += ",1,,";
                    else
                        entry += ",,,";


                }
                values.Add(entry);
                currentLine += entry;

                currentLine = "";

            }
            writeCSV(values, originalFileToParse[0]);

        }

        public void parseFiles(bool outputDifferences)
        {
            //Load the XML data for the original file
            XDocument orig = XDocument.Load(originalFileToParse[0]);

            //Loop through each of the files we want to compare the original for..
            foreach (String fname in modifiedFilesToParse)
            {
                Console.WriteLine("Parsing: " + fname);
                //This list is used to store the values we grab from the XML files. 
                //Each of the list values will eventuall be one line in our CSV file.
                List<String> values = new List<String>();

                //Load the xml data for the file we are comparing          
                XDocument doc = XDocument.Load(fname);

                //Make the first lines of our CSV file some useful headers.
                values.Add("ORIGINAL DATA,,,,,,,,CHANGED DATA");
                values.Add("shapeId, lugIndex, name, grade, price, volume,,shapeId, lugIndex, name, grade, price, volume,isDifferent");

                //Loop through all of the XML data..
                for (int j = 0; j < doc.Root.Elements().Count(); j++)
                {
                    //Flag to see if the original data(only checking name), is differnet from the other file
                    bool isDifferent = false;

                    //Create our xelement for this section of original, and modified data
                    XElement orig_data = orig.Root.Elements().ElementAt(j);
                    XElement element = doc.Root.Elements().ElementAt(j);

                    //Declare an empty string to be used as a placeholder for data before we add it to the String List values.
                    string entry = "";

                    //Loop through each element in that section of of the XML file. Add the original data, then the changed data, and then compare
                    for (int i = 0; i < element.Elements().Count(); i++)
                    {
                        try
                        {
                            //we only keep shapeId, lugIndex, name, grade, price and volume
                            if (element.Elements().ElementAt(i).Name.LocalName == "shapeId" || element.Elements().ElementAt(i).Name.LocalName == "lugIndex" || element.Elements().ElementAt(i).Name.LocalName == "name" || element.Elements().ElementAt(i).Name.LocalName == "grade" || element.Elements().ElementAt(i).Name.LocalName == "price")
                            {
                                //This is to account for a no solution 
                                if (element.Elements().ElementAt(i).Name.LocalName == "price" && orig_data.Elements().ElementAt(i).Value == "0.0")
                                    entry += ",,";
                                entry += (orig_data.Elements().ElementAt(i).Value + ",");
                            }
                            else if (element.Elements().ElementAt(i).Name.LocalName == "volume")
                            {
                                entry += (orig_data.Elements().ElementAt(i).Value + ",,");

                            }
                            //values.Add(el.Attribute("Value").Value.ToString());
                        }
                        catch { }
                    }
                    for (int i = 0; i < element.Elements().Count(); i++)
                    {
                        try
                        {
                            //we only keep shapeId, lugIndex, name, grade, price and volume
                            if (element.Elements().ElementAt(i).Name.LocalName == "shapeId" || element.Elements().ElementAt(i).Name.LocalName == "lugIndex" || element.Elements().ElementAt(i).Name.LocalName == "name" || element.Elements().ElementAt(i).Name.LocalName == "grade" || element.Elements().ElementAt(i).Name.LocalName == "price")
                            {
                                if (element.Elements().ElementAt(i).Name.LocalName == "price" && orig_data.Elements().ElementAt(i).Value == "0.0")
                                    entry += ",,";
                                entry += (element.Elements().ElementAt(i).Value + ",");
                            }
                            else if (element.Elements().ElementAt(i).Name.LocalName == "volume")
                            {
                                entry += (element.Elements().ElementAt(i).Value);

                            }
                            if (element.Elements().ElementAt(i).Name.LocalName == "name")
                            {
                                if (element.Elements().ElementAt(i).Value != orig_data.Elements().ElementAt(i).Value)
                                    isDifferent = true;
                            }
                        }
                        catch { }
                    }
                    if (isDifferent)
                        entry += ",1";
                    if (outputDifferences)
                    {
                        if (isDifferent)
                            values.Add(entry);
                    }
                    else
                    {
                        values.Add(entry);
                    }

                }
                writeCSV(values, fname);
                //write the new csv file  
            }
        }

        private void writeCSV(List<String> values, string fname)
        {
            string parentName = Path.GetFileName(fname);
            File.WriteAllLines(parentName + ".csv", values);
            Console.WriteLine("Succesfully wrote file: " + parentName + ".csv");
        }
        public void echoFileNames()
        {
            foreach (String fname in fileList)
            {
                Console.WriteLine(fname);
            }
        }
        public String[] getSimulationNames()
        {
            return fileList;
        }

        public String[] getOriginalNames()
        {
            return originalFile;
        }
    }
}
