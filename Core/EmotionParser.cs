namespace GhosTTS.Core
{
    public static class EmotionParser
    {
        private static readonly Dictionary<string, string> EmotionKeywords = new()
        {
            // Happy / Excited
            { "yay", "Happy" },
            { "awesome", "Happy" },
            { "great", "Happy" },
            { "love", "Happy" },
            { "haha", "Happy" },
            { "lol", "Happy" },
            { "🎉", "Happy" },
            { "😊", "Happy" },

            // Sad
            { "sad", "Sad" },
            { "sorry", "Sad" },
            { "miss", "Sad" },
            { "alone", "Sad" },
            { "hurt", "Sad" },
            { "💔", "Sad" },
            { "😢", "Sad" },

            // Angry
            { "angry", "Angry" },
            { "mad", "Angry" },
            { "hate", "Angry" },
            { "stupid", "Angry" },
            { "annoying", "Angry" },
            { "ugh", "Angry" },
            { "😠", "Angry" },

            // Confused / Unsure
            { "what", "Confused" },
            { "why", "Confused" },
            { "huh", "Confused" },
            { "confused", "Confused" },
            { "??", "Confused" },
            { "🤔", "Confused" },

            // Fear / Panic
            { "scared", "Fear" },
            { "help", "Fear" },
            { "run", "Fear" },
            { "afraid", "Fear" },
            { "panic", "Fear" },
            { "😱", "Fear" },

            // Love / Romantic
            { "babe", "Romantic" },
            { "baby", "Romantic" },
            { "sweet", "Romantic" },
            { "beautiful", "Romantic" },
            { "😍", "Romantic" },
            { "<3", "Romantic" }
        };
        public static string DetectEmotion(string input)
        {
            string lowered = input.ToLower();

            // Punctuation-based cues
            if (lowered.EndsWith("!") || lowered.Contains("!!!"))
                return "Excited";
            if (lowered.EndsWith("..."))
                return "Sad";
            if (lowered.Contains("?"))
                return "Confused";

            // Word-based cues
            foreach (var kvp in EmotionKeywords)
            {
                if (lowered.Contains(kvp.Key))
                    return kvp.Value;
            }

            return "Neutral";
        }
    }
}
