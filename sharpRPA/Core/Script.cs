﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Windows.Forms;
using System.Xml;

namespace sharpRPA.Core.Script
    {
        #region Script and Variables
        public class Script
        {
            public List<ScriptVariable> Variables { get; set; }

            public List<ScriptAction> Commands;
            public Script()
            {
                Variables = new List<ScriptVariable>();
                Commands = new List<ScriptAction>();
            }

            public ScriptAction AddNewParentCommand(Core.AutomationCommands.ScriptCommand scriptCommand)
            {
            ScriptAction newExecutionCommand = new ScriptAction() { ScriptCommand = scriptCommand };
                Commands.Add(newExecutionCommand);
                return newExecutionCommand;

            }

        public static Script SerializeScript(string scriptFilePath, ListView.ListViewItemCollection scriptCommands, List<ScriptVariable> scriptVariables)
        {
            //create fileStream
            var fileStream = System.IO.File.Create(scriptFilePath);

            var script = new Core.Script.Script();

            //save variables to file

            script.Variables = scriptVariables;

            //save listview tags to command list

            int lineNumber = 1;

            List<Core.Script.ScriptAction> subCommands = new List<Core.Script.ScriptAction>();

            foreach (ListViewItem commandItem in scriptCommands)
            {
                var command = (Core.AutomationCommands.ScriptCommand)commandItem.Tag;
                command.LineNumber = lineNumber;


                if (command is Core.AutomationCommands.BeginLoopCommand)
                {
                    if (subCommands.Count == 0)  //if this is the first loop
                    {
                        //add command to root node
                        var newCommand = script.AddNewParentCommand(command);
                        //add to tracking for additional commands
                        subCommands.Add(newCommand);
                    }
                    else  //we are already looping so add to sub item
                    {
                        //get reference to previous node
                        var parentCommand = subCommands[subCommands.Count - 1];
                        //add as new node to previous node
                        var nextNodeParent = parentCommand.AddAdditionalAction(command);
                        //add to tracking for additional commands
                        subCommands.Add(nextNodeParent);
                    }
                }
                else if (command is Core.AutomationCommands.EndLoopCommand)  //if current loop scenario is ending
                {
                    //get reference to previous node
                    var parentCommand = subCommands[subCommands.Count - 1];
                    //add to end command // DECIDE WHETHER TO ADD WITHIN LOOP NODE OR PREVIOUS NODE
                    parentCommand.AddAdditionalAction(command);
                    //remove last command since loop is ending
                    subCommands.RemoveAt(subCommands.Count - 1);
                }
                else if (subCommands.Count == 0) //add command as a root item
                {
                    script.AddNewParentCommand(command);
                }
                else //we are within a loop so add to the latest tracked loop item
                {
                    var parentCommand = subCommands[subCommands.Count - 1];
                    parentCommand.AddAdditionalAction(command);
                }

                //increment line number
                lineNumber++;
            }

            //output to xml file
            XmlSerializer serializer = new XmlSerializer(typeof(Script));
            var settings = new XmlWriterSettings();
            settings.NewLineHandling = NewLineHandling.Entitize;
            settings.Indent = true;

            //write to file
            using (XmlWriter writer = XmlWriter.Create(fileStream, settings))
            {
                serializer.Serialize(writer, script);
            }

            fileStream.Close();

            return script;
        }
        public static Script DeserializeFile(string scriptFilePath)
        {

            XmlSerializer serializer = new XmlSerializer(typeof(Script));
            System.IO.FileStream fs = new System.IO.FileStream(scriptFilePath, System.IO.FileMode.Open);
            XmlReader reader = XmlReader.Create(fs);
            Script deserializedData = (Script)serializer.Deserialize(reader);
            fs.Close();
            return deserializedData;
        }

        public static Script DeserializeXML(string scriptXML)
        {
            System.IO.StringReader reader = new System.IO.StringReader(scriptXML);       
            XmlSerializer serializer = new XmlSerializer(typeof(Script));
            Script deserializedData = (Script)serializer.Deserialize(reader);
            return deserializedData;
        }

    }

        public class ScriptAction
        {
            [XmlElement(Order = 1)]
            public Core.AutomationCommands.ScriptCommand ScriptCommand { get; set; }
            [XmlElement(Order = 2)]
            public List<ScriptAction> AdditionalScriptCommands = new List<ScriptAction>();

            public ScriptAction AddAdditionalAction(Core.AutomationCommands.ScriptCommand scriptCommand)
            {
            ScriptAction newExecutionCommand = new ScriptAction() { ScriptCommand = scriptCommand };
                AdditionalScriptCommands.Add(newExecutionCommand);
                return newExecutionCommand;
            }

        }

        public class ScriptVariable
        {
            public string variableName { get; set; }
            public string variableValue { get; set; }
        
    }

     #endregion
}

