public class BadWordFilter
{
    private List<string> badWords = new List<string> { "küfür1", "küfür2", "küfür3" }; // Küfürlü kelimeler

    // Mesajda küfürlü kelime olup olmadığını kontrol et
    public bool ContainsBadWord(string message)
    {
        foreach (var word in badWords)
        {
            if (message.ToLower().Contains(word.ToLower()))
            {
                return true;
            }
        }
        return false;
    }
}
