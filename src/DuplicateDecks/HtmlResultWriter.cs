using System;

namespace DuplicateDecks
{
    internal class HtmlResultWriter : IResultWriter
    {
        public void Footer()
        {
            Console.WriteLine("</body>");
            Console.WriteLine("</html>");
        }

        public void Group(int count)
        {
            Console.WriteLine("<br />");
            Console.Write(count);
            Console.WriteLine("<br />");
        }

        public void Header()
        {
            Console.WriteLine("<html>");
            Console.WriteLine("<head>");
            Console.WriteLine("</head>");
            Console.WriteLine("<body>");
        }

        public void Item(string title)
        {
            Console.WriteLine($"<a href=\"https://magicarena.fandom.com/wiki/{title}\" target=\"_blank\">{title}</a><br />");
        }
    }
}
