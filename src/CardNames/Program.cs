using GathererShared;
using Newtonsoft.Json;
using NHunspell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CardNames
{
    class Program
    {
        static char[] space = { ' ' };

        static void Main(string[] args)
        {
            TextReader input = (args.Length == 0) ? Console.In : new StreamReader(args[0]);

            char[] unwanted = { ' ', '-' };
            using (var hunspell = new Hunspell("en_us.aff", "en_us.dic"))
            {
                var cards = GetCardData("Cards.json");
                AddCardNameWordsToSpellChecker(hunspell, cards);
                string line;
                while ((line = input.ReadLine()) != null)
                {
                    var lineWords = new List<List<string>>();
                    string[] words = line.Split(space, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var word in words)
                    {
                        var wordList = new List<string>();
                        AddPossibleWords(hunspell, word, wordList);
                        if (IsPlural(word))
                            AddPossibleWords(hunspell, word.Substring(0, word.Length - 1), wordList);
                        lineWords.Add(wordList);
                    }
                    var lineOut = new StringBuilder();
                    int wordIndex = 0;
                    string suffix;
                    while (wordIndex < words.Length)
                    {
                        string[] nameWords = GetCardName(cards, words, lineWords, wordIndex, hunspell, out suffix);
                        if (nameWords != null)
                        {
                            lineOut.Append("{{Card|");
                            for (int k = 0; k < nameWords.Length; k++)
                            {
                                lineOut.Append(nameWords[k]);
                                lineOut.Append(' ');
                            }
                            lineOut.Remove(lineOut.Length - 1, 1);
                            lineOut.Append("}}");
                            if (!IsPlural(nameWords[nameWords.Length - 1]) && IsPlural(words[wordIndex + nameWords.Length - 1]))
                                lineOut.Append('s');
                            lineOut.Append(suffix);
                            lineOut.Append(' ');
                            wordIndex += nameWords.Length;
                        }
                        else
                        {
                            lineOut.Append(words[wordIndex++]); // original word
                            lineOut.Append(' ');
                        }
                    }
                    Console.WriteLine(lineOut);
                }
            }
        }

        private static bool IsPlural(string word)
        {
            if (word.Length < 2)
                return false;
            char ch = char.ToUpperInvariant(word[word.Length - 1]);
            return ch == 'S';
        }

        private static void AddPossibleWords(Hunspell hunspell, string word, List<string> wordList)
        {
            if (hunspell.Spell(word))
            {
                wordList.Add(word);
            }
            else
            {
                List<string> sug = hunspell.Suggest(word);
                if (sug.Count > 0)
                    wordList.AddRange(sug);
                else
                    wordList.Add(word);
            }
        }

        private static string[] GetCardName(List<Card> cards, string[] originalWords, List<List<string>> lineWords, int wordIndex, Hunspell hunspell, out string suffix)
        {
            suffix = "";
            foreach (var card in cards)
            {
                suffix = "";
                var nameWords = card.Name.Split(' ');
                bool isName = true;
                int lastWordIndex = nameWords.Length - 1;
                for (int k = 0; k <= lastWordIndex; k++)
                {
                    List<string> word = lineWords[wordIndex + k];
                    if (k == lastWordIndex)
                    {
                        string originalWord = originalWords[wordIndex + k];
                        char ch = originalWord[originalWord.Length - 1];
                        if (!char.IsLetter(ch))
                        {
                            suffix += ch;
                        }
                    }
                    if (k == lastWordIndex && word.Count == 1 && suffix != "")
                    {
                        if (!word[0].Substring(0, word[0].Length - 1).Equals(nameWords[k], StringComparison.InvariantCultureIgnoreCase))
                        {
                            isName = false;
                            break; // for int k
                        }
                    }
                    else if (!word.ContainsIgnoreCase(nameWords[k]))
                    {
                        isName = false;
                        break; // for int k
                    }
                }
                if (isName)
                    return nameWords;
            }
            return null;
        }

        private static void AddCardNameWordsToSpellChecker(Hunspell hunspell, List<Card> cards)
        {
            var unknownWords = new HashSet<string>();
            foreach (var card in cards)
            {
                string[] words = card.Name.Split(' ');
                foreach (var word in words)
                {
                    if (!hunspell.Spell(word))
                    {
                        unknownWords.Add(word);
                    }
                }
            }
            foreach (var word in unknownWords)
                hunspell.Add(word);
        }

        private static string[] GetCardName(List<Card> cards, string[] words, int wordIndex, Hunspell hunspell)
        {
            foreach (var card in cards)
            {
                var nameWords = card.Name.Split(' ');
                bool isName = true;
                for (int k = 0; k < nameWords.Length; k++)
                {
                    string word = words[wordIndex + k];
                    if (hunspell.Spell(word))
                    {
                        if (word != nameWords[k])
                        {
                            isName = false;
                            break; // for int k
                        }
                    }
                    else
                    {
                        var altwords = hunspell.Suggest(word);
                        if (!altwords.Contains(nameWords[k]))
                        {
                            isName = false;
                            break; // for int k
                        }
                    }
                }
                if (isName)
                    return nameWords;
            }
            return null;
        }

        private static List<Card> GetCardData(string fileName)
        {
            List<Card> cards;
            string json = File.ReadAllText(fileName);
            cards = JsonConvert.DeserializeObject<List<Card>>(json);
            return cards;
        }

    }
}
