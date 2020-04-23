using System.Collections.Generic;

namespace AndroidBot
{
    public struct DictionaryApiResponse
    {
        public string word;
        public Dictionary<string, Meaning[]> meaning;

        public struct Meaning
        {
            public string definition;
        }
    }
}