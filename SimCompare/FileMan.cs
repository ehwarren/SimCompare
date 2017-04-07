using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Linq;

namespace SimCompare
{
    class FileMan
    {
        private String[] originalFile;
        private String[] fileList;
        public String[] originalFileToParse { get; set; }
        public String[] modifiedFilesToParse { get; set; }
        public bool useZPosAsDifference;
        public bool openFolderAfterCompare;

        static string seperator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;

        public FileMan()
        {
            refreshFiles();
            useZPosAsDifference = true;
            openFolderAfterCompare = true;
        }

        public void refreshFiles()
        {
            try
            {
                originalFile = Directory.GetFiles("./original/", "*.xml").OrderByDescending(d => new FileInfo(d).LastWriteTime).ToArray();
            }
            catch
            {
                Console.WriteLine("Could not open original xml file...");
            }
            try
            {
                fileList = Directory.GetFiles("./changes/", "*.xml").OrderByDescending(d => new FileInfo(d).LastWriteTime).ToArray();
            }
            catch
            {
                Console.WriteLine("Could not open changed xml file...");
            }
        }

        public void parseFilesInOne(object sender, EventArgs eventArgs)
        {
            //Load the XML data for the original file
            XDocument orig = new XDocument();
            try
            {
                orig = Sort(XDocument.Load(originalFileToParse[0]));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            //Load the xml data for each of the simulations
            XDocument[] changes = new XDocument[modifiedFilesToParse.Length];
            for (int i = 0; i < modifiedFilesToParse.Length; i++)
            {
                try
                {
                    changes[i] = Sort(XDocument.Load(modifiedFilesToParse[i]));
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }

            }

            //This list is used to store the values we grab from the XML files. 
            //Each of the list values will eventuall be one line in our CSV file.
            List<String> values = new List<String>();

            //The following is used to track the volume and price of each grade in each simulation
            List<String>[] grades = new List<String>[modifiedFilesToParse.Length + 1];
            Dictionary<String, int>[] numberOfPieces = new Dictionary<string, int>[modifiedFilesToParse.Length + 1];
            Dictionary<String, float>[] volumeOfPieces = new Dictionary<string, float>[modifiedFilesToParse.Length + 1];
            List<int> numberOfLugs = new List<int>();
            List<int> numberOfLugsWithDifferentSolution = new List<int>();
            List<float> totalPrice = new List<float>();
            List<float> totalVolume = new List<float>();


            String currentLine = "";
            //Add the tags for each of the simulations
            currentLine = "ORIGINAL DATA" + addSeperator(8);
            for (int i = 0; i < modifiedFilesToParse.Length; i++)
            {
                currentLine += "SIMULATION ";
                currentLine += i + 1;
                currentLine += " " + modifiedFilesToParse[i];
                currentLine += addSeperator(9);
            }
            values.Add(currentLine);
            //add the column headers for each of the simulations
            currentLine = "shapeId" + seperator +
                            " lugIndex" + seperator +
                            " name" + seperator + 
                            " grade" + seperator +
                            " price" + seperator + 
                            " volume" + seperator +
                            " zPosition" + addSeperator(2);
            grades[0] = new List<string>();
            numberOfPieces[0] = new Dictionary<string, int>();
            volumeOfPieces[0] = new Dictionary<string, float>();
            totalPrice.Add(0);
            totalVolume.Add(0);
            numberOfLugs.Add(0);
            numberOfLugsWithDifferentSolution.Add(0);
            for (int i = 0; i < modifiedFilesToParse.Length; i++)
            {
                currentLine += "shapeId" + seperator +
                                " lugIndex" + seperator +
                                " name" + seperator + 
                                " grade" + seperator +
                                " price" + seperator +
                                " volume" + seperator +
                                " zPosition" + seperator +
                                " isDifferent" + addSeperator(2);
                grades[i+1] = new List<string>();
                numberOfPieces[i+1] = new Dictionary<string, int>();
                volumeOfPieces[i+1] = new Dictionary<string, float>();
                totalPrice.Add(0);
                totalVolume.Add(0);
                numberOfLugs.Add(0);
                numberOfLugsWithDifferentSolution.Add(0);
            }
            values.Add(currentLine);
            //clear our currentLine variable because we've added it to the list already
            currentLine = "";
            
            //Verify that the amount of data in the original and all simualtion files matches
            foreach(XDocument d in changes)
            {
                if (d.Root.Elements().Count() != orig.Root.Elements().Count())
                    MessageBox.Show("ERROR: Number of boards in Simulation does not match Original");
            } 

            //Loop through each of the files we want to compare the original for..
            Console.WriteLine("Parsing the files...");
            Console.WriteLine(orig.Root.Elements().Count());

            int[] lastLugIndexUsedForDifference = new int[changes.Length];
            int[] lastLugIndexUsedForCount = new int[changes.Length];

            //Loop through all of the XML data..
            for (int j = 0; j < orig.Root.Elements().Count(); j++)
            {
                float prog = (((float)j) / orig.Root.Elements().Count())*100;
                (sender as BackgroundWorker).ReportProgress((int)prog);
                //Flag to see if the original data(only checking name), is differnet from the other file
                bool isDifferent = false;

                //Create our xelement for this section of original, and modified data
                //XElement orig_data = (XElement)orig_ordered.Elements().ElementAt(j);
                XElement orig_data = orig.Root.Elements().ElementAt(j);
                //Declare an empty string to be used as a placeholder for data before we add it to the String List values.
                string entry = "";

                try
                {
                    entry += orig_data.Element("shapeId").Value + seperator;
                    entry += orig_data.Element("lugIndex").Value + seperator;

                    //To account for no solution
                    if (orig_data.Element("price").Value != "0.0")
                    {
                        entry += orig_data.Element("name").Value + seperator;
                        entry += orig_data.Element("grade").Value + seperator;
                    }
                    else
                    {
                        entry += addSeperator(2);
                    }

                    entry += orig_data.Element("price").Value + seperator;
                    entry += orig_data.Element("volume").Value + seperator;
                    entry += orig_data.Element("zPosition").Value + addSeperator(2);

                    if (orig_data.Elements("grade").Any())
                    {
                        if (!grades[0].Contains(orig_data.Element("grade").Value))
                        {
                            grades[0].Add(orig_data.Element("grade").Value);
                            numberOfPieces[0][orig_data.Element("grade").Value] = 0;
                            volumeOfPieces[0][orig_data.Element("grade").Value] = 0;
                        }
                        numberOfPieces[0][orig_data.Element("grade").Value]++;
                        volumeOfPieces[0][orig_data.Element("grade").Value] += float.Parse(orig_data.Element("volume").Value);
                    }

                    numberOfLugs[0]++;
                    totalPrice[0] += float.Parse(orig_data.Element("price").Value);
                    totalVolume[0] += float.Parse(orig_data.Element("volume").Value);

                }
                catch { }

                int numSim = 1;
                //now we loop through each of our changed files and add the entry to the list
                for (int sim = 0; sim < changes.Length; sim++)
                {
                    isDifferent = false;    //BugFix1 - Didn't clear the variable, so if the first file compared was different it would assume all the rest were too...
                    XElement sim_data = changes[sim].Root.Elements().ElementAt(j);
                    try
                    {
                        entry += sim_data.Element("shapeId").Value + seperator;
                        entry += sim_data.Element("lugIndex").Value + seperator;

                        //To account for no solution
                        if (sim_data.Element("price").Value != "0.0")
                        {
                            entry += sim_data.Element("name").Value + seperator;
                            entry += sim_data.Element("grade").Value + seperator;
                        }
                        else
                        {
                            entry += addSeperator(2);
                        }

                        entry += sim_data.Element("price").Value + seperator;
                        entry += sim_data.Element("volume").Value + seperator;
                        entry += sim_data.Element("zPosition").Value + seperator;

                        if (sim_data.Elements("name").Any() && orig_data.Elements("name").Any())
                        {
                            //If they both contain a name, we need to see if that name changed to mark it as different.
                            if (sim_data.Element("name").Value != orig_data.Element("name").Value)
                                isDifferent = true;
                        }
                        else if ( sim_data.Elements("name").Any() || orig_data.Elements("name").Any() )
                        {
                            //If one contains a name and the other doesn't this means that we had a cut-n-two in 1 and not in the other.
                            isDifferent = true;
                        }
                        if (useZPosAsDifference == true)
                        {
                            if (sim_data.Elements("zPosition").Any() && orig_data.Elements("zPosition").Any())
                            {
                                if (sim_data.Element("zPosition").Value != orig_data.Element("zPosition").Value)
                                    isDifferent = true;
                            }
                        }

                        if(sim_data.Elements("grade").Any())
                        {
                            if (!grades[numSim].Contains(sim_data.Element("grade").Value))
                            {
                                grades[numSim].Add(sim_data.Element("grade").Value);
                                numberOfPieces[numSim][sim_data.Element("grade").Value] = 0;
                                volumeOfPieces[numSim][sim_data.Element("grade").Value] = 0;
                            }
                            numberOfPieces[numSim][sim_data.Element("grade").Value]++;
                            volumeOfPieces[numSim][sim_data.Element("grade").Value] += float.Parse(sim_data.Element("volume").Value);
                        }

                        totalPrice[numSim] += float.Parse(sim_data.Element("price").Value);
                        totalVolume[numSim] += float.Parse(sim_data.Element("volume").Value);

                        //Check to see if we need to increment our numberOfLugsWithDifferentSolution counter
                        int currentLugIndex = int.Parse(sim_data.Element("lugIndex").Value);
                        if (lastLugIndexUsedForCount[numSim-1] != currentLugIndex)
                        {
                            numberOfLugs[numSim]++;
                            lastLugIndexUsedForCount[numSim - 1] = currentLugIndex;
                        }
                        if (lastLugIndexUsedForDifference[numSim - 1] != currentLugIndex)
                        {
                            if(isDifferent)
                            {
                                numberOfLugsWithDifferentSolution[numSim]++;
                                lastLugIndexUsedForDifference[numSim - 1] = currentLugIndex;
                            }
                        }
                        
                        
                    }
                    catch { }
                    if (isDifferent)
                    {
                        entry += "1" + addSeperator(2);   
                    }
                    else
                    {
                        entry += addSeperator(2);
                    }

                    numSim++;
                }

                values.Add(entry);
                currentLine += entry;

                currentLine = "";

            }
            //Now we add the volume/piece count data to the report
            values.Add("");
            currentLine = addSeperator(3) + "Total Price" + addSeperator(2) + "Total Volume" + addSeperator(3);
            for (int i = 0; i < changes.Length; i++)
            {
                currentLine += addSeperator(3) + "Total Price" + addSeperator(2) + "Total Volume" + addSeperator(4);
            }
            values.Add(currentLine);

            //Add total price/volume
            currentLine = "";
            for(int i=0; i<numberOfLugs.Count; i++)
            {
                currentLine += addSeperator(3);
                currentLine += totalPrice[i];
                currentLine += addSeperator(2);
                currentLine += totalVolume[i];
                currentLine += addSeperator(3);
                if (i > 0)
                    currentLine += seperator;
            }
            values.Add(currentLine);

            //Add changes in price/volume
            currentLine = "";
            for (int i = 0; i < numberOfLugs.Count; i++)
            {
                if(i>0)
                {
                    currentLine += addSeperator(2) + "Change:" + seperator;
                    currentLine += totalPrice[i] - totalPrice[0];
                    currentLine += addSeperator(2);
                    currentLine += totalVolume[i] - totalVolume[0];
                    currentLine += addSeperator(4);
                }
                else
                {
                    currentLine += addSeperator(8);
                }
            }
            values.Add(currentLine);
            values.Add("");
            values.Add("");

            //Add total number of lugs
            currentLine = "";
            for (int i = 0; i < numberOfLugs.Count; i++)
            {
                if (i > 0)
                {
                    currentLine += addSeperator(2) + "Number of lugs in this sample:" + addSeperator(3);
                    currentLine += numberOfLugs[i];
                    currentLine += addSeperator(4);
                }
                else
                {
                    currentLine += addSeperator(8);
                }
            }
            values.Add(currentLine);

            //Add total number of changes
            currentLine = "";
            for (int i = 0; i < numberOfLugs.Count; i++)
            {
                if (i > 0)
                {
                    currentLine += seperator + "Number of lugs with different solution:" + addSeperator(4);
                    currentLine += numberOfLugsWithDifferentSolution[i];
                    currentLine += addSeperator(4);
                }
                else
                {
                    currentLine += addSeperator(8);
                }
            }
            values.Add(currentLine);

            //Add percentage of changes
            currentLine = "";
            for (int i = 0; i < numberOfLugs.Count; i++)
            {
                if (i > 0)
                {
                    currentLine += seperator + "Percent of lugs with different solution:" + addSeperator(4);
                    currentLine += ((float)numberOfLugsWithDifferentSolution[i]/numberOfLugs[i])*100;
                    currentLine += "%";
                    currentLine += addSeperator(4);
                }
                else
                {
                    currentLine += addSeperator(8);
                }
            }
            values.Add(currentLine);
            values.Add("");
            values.Add("");
            values.Add("");
            currentLine = addSeperator(3) + "Number PCS" + addSeperator(2) + "Volume of Grade" + addSeperator(3);
            for(int i=0; i<changes.Length; i++)
            {
                currentLine += addSeperator(3) + "Number PCS" + addSeperator(2) + "Volume of Grade" + addSeperator(4);
            }
            values.Add(currentLine);

            bool stillCounting = true;
            int currentGrade = 0;
            while (stillCounting)
            {
                currentLine = addSeperator(2);
                stillCounting = false;
                for(int currentSim = 0; currentSim < grades.Length; currentSim++)
                {
                    if (currentGrade < grades[currentSim].Count)
                    {
                        currentLine += grades[currentSim][currentGrade];
                        currentLine += seperator + numberOfPieces[currentSim][grades[currentSim][currentGrade]];
                        currentLine += addSeperator(2) + volumeOfPieces[currentSim][grades[currentSim][currentGrade]];
                        currentLine += addSeperator(3);
                        stillCounting = true;
                    }
                    else
                    {
                        currentLine += addSeperator(6);
                    }
                    currentLine += addSeperator(2);
                    if (currentSim > 0)
                        currentLine += seperator;
                }
                values.Add(currentLine);
                currentGrade++;
                
            }
            string outputDir = Directory.GetCurrentDirectory() + "/" + Constants.OUTPUT_FOLDER;
            MessageBox.Show(writeCSV(values, originalFileToParse[0]));
            if(openFolderAfterCompare)
            {
                //Open a folder to the output directory
                Process.Start(outputDir);
            }

        }

        public string parseFiles(bool outputDifferences)
        {
            //Load the XML data for the original file
            XDocument orig;
            XDocument doc;

            try
            {
                orig = Sort(XDocument.Load(originalFileToParse[0]));
            }
            catch (Exception e)
            {
                return e.Message;
            }

            //Loop through each of the files we want to compare the original for..
            foreach (String fname in modifiedFilesToParse)
            {
                Console.WriteLine("Parsing: " + fname);
                //This list is used to store the values we grab from the XML files. 
                //Each of the list values will eventuall be one line in our CSV file.
                List<String> values = new List<String>();

                //Load the xml data for the file we are comparing   
                try
                {
                    doc = Sort(XDocument.Load(fname));
                }
                catch (Exception e)
                {
                    return e.Message;
                }

                //Make the first lines of our CSV file some useful headers.
                values.Add("ORIGINAL DATA" + seperator + seperator + seperator + seperator + seperator + seperator + seperator + seperator + "CHANGED DATA");
                values.Add("shapeId" + seperator +
                            " lugIndex" + seperator +
                            " name" + seperator +
                            " grade" + seperator +
                            " price" + seperator +
                            " volume" + seperator + seperator +
                            " shapeId" + seperator +
                            " lugIndex" + seperator +
                            " name" + seperator +
                            " grade" + seperator +
                            " price" + seperator +
                            " volume" + seperator +
                            " isDifferent");

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
                                    entry += seperator + seperator;
                                entry += (orig_data.Elements().ElementAt(i).Value + seperator);
                            }
                            else if (element.Elements().ElementAt(i).Name.LocalName == "volume")
                            {
                                entry += (orig_data.Elements().ElementAt(i).Value + seperator + seperator);

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
                                    entry += seperator + seperator;
                                entry += (element.Elements().ElementAt(i).Value + seperator);
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
                        entry += seperator + "1";
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
            return "Wrote all files succesfully";
        }

        private String writeCSV(List<String> values, string fname)
        {
            string parentName = Path.GetFileName(fname);
            string savePath = Constants.OUTPUT_FOLDER + "/" + parentName + ".csv";
            try
            {
                (new FileInfo(savePath)).Directory.Create();
                File.WriteAllLines(savePath, values);
            }
            catch(Exception e)
            {
                return e.Message;
            }
            
            return savePath;
        }
        public String[] getSimulationNames()
        {
            return fileList;
        }

        public String[] getOriginalNames()
        {
            return originalFile;
        }

        private static XDocument Sort(XDocument file)
        {
            var tt = file.Element("Data")
               .Elements("Solution")
               .OrderByDescending(p => p.Element("lugIndex").Value);
            return new XDocument(new XElement("Data", tt));
        }

        private string addSeperator(int n)
        {
            string temp = "";
            for(int i=0; i<n; i++)
            {
                temp += seperator;
            }
            return temp;
        }
    }
}
