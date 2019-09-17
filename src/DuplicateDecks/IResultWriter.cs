namespace DuplicateDecks
{
    interface IResultWriter
    {
        void Header();
        void Footer();
        void Group(int count);
        void Item(string title);
    }
}
