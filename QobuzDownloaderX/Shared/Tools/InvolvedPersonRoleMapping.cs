using System;
using System.Collections.Generic;
using System.Linq;

namespace QobuzDownloaderX.Shared
{
    public static class InvolvedPersonRoleMapping
    {
        private static readonly Dictionary<InvolvedPersonRoleType, List<string>> RoleMappings = new Dictionary<InvolvedPersonRoleType, List<string>>
        {
            { InvolvedPersonRoleType.Miscellaneous, new List<string> { "A&R Director", "A&R", "AAndRAdministrator", "Additional Production",
                "AHH", "Assistant Mixer", "AssistantEngineer", "Asst. Recording Engineer", "AssociatedPerformer", "Author", "Bass Guitar", "Co-Producer",
                "Drums", "Engineer", "Guitar", "Keyboards", "Masterer", "Mastering Engineer", "MasteringEngineer", "Misc.Prod.", "Mixer", "Mixing Engineer",
                "Music Production", "Orchestra", "Percussion", "Programming", "Programmer", "RecordingEngineer", "Soloist", "StudioPersonnel", "Trumpet",
                "Violin", "Vocals", "Writer"} }, // Default if not mapped!
            { InvolvedPersonRoleType.Composer, new List<string> { "Composer", "ComposerLyricist", "Composer-Lyricist" } },
            { InvolvedPersonRoleType.Conductor, new List<string> { "Conductor" } },
            { InvolvedPersonRoleType.FeaturedArtist, new List<string> {"FeaturedArtist", "Featuring", "featured-artist" } },
            { InvolvedPersonRoleType.Lyricist, new List<string> { "Lyricist", "ComposerLyricist", "Composer-Lyricist" } },
            { InvolvedPersonRoleType.MainArtist, new List<string> { "MainArtist", "main-artist" } },
            { InvolvedPersonRoleType.MixArtist, new List<string> { "Remixer", "Re-Mixer"} },
            { InvolvedPersonRoleType.Producer, new List<string> { "Producer"} },
            { InvolvedPersonRoleType.Publisher, new List<string> { "Publisher", "MusicPublisher" } }
        };

        public static List<string> GetStringsByRole(InvolvedPersonRoleType role)
        {
            if (RoleMappings.TryGetValue(role, out var strings))
            {
                return strings;
            }
            return new List<string>();
        }

        public static InvolvedPersonRoleType GetRoleByString(string involvedPersonString)
        {
            // We just return PerformerRoleType.Miscellaneous if no match as we don't know all possible values
            // Search is done by ignoring case for maximum success.
            return RoleMappings.FirstOrDefault(kvp => kvp.Value.Contains(involvedPersonString, StringComparer.OrdinalIgnoreCase)).Key;
        }
    }
}