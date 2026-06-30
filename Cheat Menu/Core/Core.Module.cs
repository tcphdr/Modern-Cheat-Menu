namespace Modern_Cheat_Menu
{
    public partial class Core
    {
        public enum EQuality
        {
            Trash = 0,
            Poor = 1,
            Standard = 2,
            Premium = 3,
            Heavenly = 4
        }

        public enum ParameterType
        {
            Input,
            Dropdown
        }

        public class CommandParameter
        {
            public string Name { get; set; }
            public string Placeholder { get; set; }
            public ParameterType Type { get; set; }
            public string ItemCacheKey { get; set; }
            public string Value { get; set; }
        }

        public class Command
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public System.Action<string[]> Handler { get; set; }
            public List<CommandParameter> Parameters { get; set; } = new List<CommandParameter>();
        }

        public class CommandCategory
        {
            public string Name { get; set; }
            public List<Command> Commands { get; set; } = new List<Command>();
        }

        public class NetworkPlayerCategory
        {
            public string Name { get; set; }
            public List<Command> Commands { get; set; } = new List<Command>();
        }
    }
}
