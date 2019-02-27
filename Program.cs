﻿using AngleSharp;
using AngleSharp.Html.Dom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using AngleSharp.Html.Parser;
using System.Text.RegularExpressions;
using AngleSharp.Dom;

namespace AngleShardDemo1
{
    public class RegexFactory
    {

        public static string FontSize { get; } = "font-size([^px]*px;)";
        public static string Color { get; } = "(?<!-)color.*?;";
        public static string BackGroundColor { get; } = "background-color:(.*?);";
        public static string FontFamily { get; } = "font-family:(.*?);";
        public static string PickParentAttributes { get; } = "<(.*?)>";
        public static string PickStyle { get; } = "(?<=style=\")(.*)(?=\")";
        
        public static Regex CreateRegex(string args) => new Regex(args);
    }


    class Program
    {
        static void Main(string[] args)
        {

            using (StreamReader reader = new StreamReader("C:\\templates\\courtyard.html"))
            {
                string content = reader.ReadToEnd();

                var parser = new HtmlParser();
                var document = parser.ParseDocument(content);
                //TestMod(document);


                StringBuilder attributesString = new StringBuilder();

                var elements = ListElementForModification(document);
                MainEngine(attributesString, elements);
                //elements.ForEach(element =>  MainEngine(attributesString, element)); 



                var final = document.DocumentElement.OuterHtml;
              
                Console.WriteLine(final);
            }

            Console.ReadLine();

        }

        private static List<IElement> ListElementForModification(IHtmlDocument document)
        {

            // use a main StringBuilder and add and remove string to it.
            var ElementsForModification = new List<IElement>()
            {
                document.GetElementById("__DESCRIPTION__"),
                document.GetElementById("__MANUFACTURER__")
            };

            void add(IHtmlCollection<IElement> elements)
            {
                foreach (var element in elements)
                {
                    ElementsForModification.Add(element);
                }
            }

            add(document.GetElementsByName("__FEATURES__"));
            add(document.GetElementsByName("__DESCRIPTION__"));
            add(document.GetElementsByName("__MANUFACTURER__"));

            return ElementsForModification;
            // pass the elements list to prepare for modification
        }

        // Sanitizer MainEngine
        private static void MainEngine(StringBuilder elementString,List<IElement> elements)
        {
            foreach (var element in elements)
            {
                if (element == null)
                {
                    return;
                }
                else
                {
                    
                    elementString.Clear();
                    string inner = element.InnerHtml;

                    // pick the parent attributes

                    var pickTheParentRegex = RegexFactory.CreateRegex(RegexFactory.PickParentAttributes);
                    string parentElement = pickTheParentRegex.Match(element.OuterHtml).Value;

                    bool parentHasStyle = parentElement.Contains("style");

                    if (!parentHasStyle)
                    {
                        element.SetAttribute("style", "");
                    }

                    string newParent = pickTheParentRegex.Match(element.OuterHtml).Value; // Outer
                    // pick the parent

                    var PickStyle = RegexFactory.CreateRegex(RegexFactory.PickStyle);
                    string parentStyle = PickStyle.Match(newParent).Value;

                    elementString.Append(parentStyle); // has to pick only the values contains an empty string or the values of the style attribute


                    //Refactor checkers

                    Dictionary <string, string> cssAtrributes = new Dictionary<string, string>()
                    {
                        { "background-color", RegexFactory.BackGroundColor },
                        { "color", RegexFactory.Color },  
                        { "font-family", RegexFactory.FontFamily },  
                        { "font-size", RegexFactory.FontFamily },
                        { "<b>", "font-weight: bold;" },
                        { "<u>", "text-decoration: underline" },
                        { "<i>", "font-style: italics" },
                        { "<strike>", "text-decoration: strike-through" }         
                    };

                    foreach(KeyValuePair<string, string> entry in cssAtrributes)
                    {
                        if (inner.Contains(entry.Key))
                        {
                            if (entry.Key == "<b>" | entry.Key == "<u>" | entry.Key == "<strike>" | entry.Key == "<i>")
                            {
                                // assumes that the entry does not exists to the outer css!
                                elementString.Insert(0, entry.Value);
                            }else
                            {
                                var regex = RegexFactory.CreateRegex(entry.Value);

                                string outerHtmlCssAttribute = regex.Match(parentStyle).Value;
                                string innerHtmlCssAttribute = regex.Match(inner).Value;
                                if (outerHtmlCssAttribute == "")
                                {
                                    elementString.Insert(0, innerHtmlCssAttribute);
                                }else
                                {
                                    elementString.Replace(outerHtmlCssAttribute, innerHtmlCssAttribute);
                                }
                            }

                        }
                    }
                    element.SetAttribute("style", elementString.ToString());
                }
            }
        }

        static void TestMod(IHtmlDocument document)
        {
            var description = document.GetElementById("__DESCRIPTION__");

            var styleRegex = new Regex("style=\"([^\"]+\")");

            // used by the string builder to insert the style to the correct index
            string parentStyle = styleRegex.Match(description.OuterHtml).Value;
            int semiIndex = parentStyle.IndexOf(";"); // find the index of the first semicolon


            StringBuilder styleOfParentBuilder = new StringBuilder(styleRegex.Match(description.OuterHtml).Value);


            string inner = description.InnerHtml;
            bool isBold = inner.Contains("<b>");

            if (isBold)
            {
                styleOfParentBuilder.Insert(semiIndex, "; font-weight: bold "); // inserts the attribute to the style
                string finalStyle = styleOfParentBuilder.ToString();
                var splitStyleOfParent = finalStyle.Split('"');

                description.SetAttribute("style", splitStyleOfParent[1]);
            }

        }

    }

}
