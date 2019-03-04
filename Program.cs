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
    internal class RegexFactory
    {

        public static string FontSize { get; } = "font-size([^px]*px;)";
        public static string FontWeight { get; } = "font-weight([^px]*px;)";

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

            using (StreamReader reader = new StreamReader("C:\\templates\\test.html"))
            {
                string content = reader.ReadToEnd();

                var parser = new HtmlParser();
                var document = parser.ParseDocument(content);

                var description = document.GetElementById("__DESCRIPTION__");


                // var elements = ListElementForModification(document);
                // MainEngine(elements);
                // List<IElement> elements = new List<IElement>();
                StringBuilder styles = new StringBuilder();
                RecursiveEngine(styles, description);


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

            var tags = new String[] { "__FEATURES__", "__DESCRIPTION__", "__MANUFACTURER__" };
            foreach(var tag in tags)
            {
                ElementsForModification.AddRange(document.GetElementsByName(tag).AsEnumerable());
            }
            ElementsForModification.RemoveAll(element => element == null);
            return ElementsForModification;
        }

        private static void RecursiveEngine(StringBuilder styles, IElement parent)
        { 
            foreach (var element in parent.Children)
            {
                styles.Append(element.GetAttribute("style") + "; ");
                RecursiveEngine(styles, element);
            }
        }


        // Sanitizer MainEngine
        private static void MainEngine(List<IElement> elements)
        {
            var attributesBuilder = new StringBuilder();
            foreach (var element in elements)
            {

                //  PICK THE STYLE OF THE OUTER ELEMENT
                var parentAttributes = element.Attributes;

                // convert to a method
                string style = "";
                foreach (var attribute in parentAttributes)
                {
                    if (attribute.Name == "style")
                    {
                        style = attribute.Value;
                    }
                }

                // APPEND THE STYLE ATTRIBUTES TO THE BUILDER
                attributesBuilder.Append(style);

                // GRAP ALL THE CHILDREN STYLES
                attributesBuilder.Clear();
                var children = element.Children.AsEnumerable();
                var childrenStyles = new StringBuilder();
                foreach (var child in children)
                {
                    var attributes = child.Attributes;
                    foreach (var attribute in attributes)
                    {
                        if (attribute.Name == "style")
                        {
                            childrenStyles.Append(attribute.Value);
                        }
                    }
                }
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

                attributesBuilder.Append(parentStyle); // has to pick only the values contains an empty string or the values of the style attribute


                //Refactor checkers

                Dictionary <string, string> cssAtrributes = new Dictionary<string, string>()
                {
                    { "background-color", RegexFactory.BackGroundColor },
                    { "color", RegexFactory.Color },  
                    { "font-family", RegexFactory.FontFamily },  
                    { "font-size", RegexFactory.FontFamily },
                    { "font-weight", RegexFactory.FontWeight },
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
                            // assumes that the entry does not exists to the parent element css attributes!
                            attributesBuilder.Insert(0, entry.Value);
                        }else
                        {
                            var regex = RegexFactory.CreateRegex(entry.Value);

                            string outerHtmlCssAttribute = regex.Match(parentStyle).Value;
                            string innerHtmlCssAttribute = regex.Match(inner).Value;
                            if (outerHtmlCssAttribute == "")
                            {
                                attributesBuilder.Insert(0, innerHtmlCssAttribute);
                            }else
                            {
                                attributesBuilder.Replace(outerHtmlCssAttribute, innerHtmlCssAttribute);
                            }
                        }

                    }
                }
                element.SetAttribute("style", attributesBuilder.ToString());
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
