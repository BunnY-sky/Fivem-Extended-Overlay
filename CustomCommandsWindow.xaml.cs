using System.Collections.ObjectModel;
using System.Windows;

namespace FiveMOverlay;

public partial class CustomCommandsWindow : Window
{
    private readonly ObservableCollection<ChatCommandSetting> _commands;
    public List<ChatCommandSetting>? ResultCommands { get; private set; }

    public CustomCommandsWindow(IEnumerable<ChatCommandSetting> commands)
    {
        InitializeComponent();
        _commands = new ObservableCollection<ChatCommandSetting>(commands.Select(c => new ChatCommandSetting
        {
            Trigger = c.Trigger,
            Replacement = c.Replacement
        }));
        CommandsGrid.ItemsSource = _commands;
    }

    private void AddExample_Click(object sender, RoutedEventArgs e)
    {
        _commands.Add(new ChatCommandSetting { Trigger = "/beispiel", Replacement = "/report {cursor}" });
        CommandsGrid.SelectedIndex = _commands.Count - 1;
        CommandsGrid.ScrollIntoView(CommandsGrid.SelectedItem);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        CommandsGrid.CommitEdit();
        CommandsGrid.CommitEdit();
        var cleaned = new List<ChatCommandSetting>();
        var triggers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var command in _commands)
        {
            string trigger = (command.Trigger ?? string.Empty).Trim();
            string replacement = command.Replacement ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trigger) && string.IsNullOrWhiteSpace(replacement)) continue;
            if (!trigger.StartsWith('/') || trigger.Length < 2 || trigger.Any(char.IsWhiteSpace))
            {
                DialogStatus.Text = $"Ungültiger Auslöser: '{trigger}'. Er muss mit / beginnen und darf keine Leerzeichen enthalten.";
                return;
            }
            if (string.IsNullOrWhiteSpace(replacement))
            {
                DialogStatus.Text = $"Für {trigger} fehlt der Einfügetext.";
                return;
            }
            if (!triggers.Add(trigger))
            {
                DialogStatus.Text = $"Der Auslöser {trigger} ist doppelt vorhanden.";
                return;
            }
            cleaned.Add(new ChatCommandSetting { Trigger = trigger, Replacement = replacement });
        }
        if (cleaned.Count == 0)
        {
            DialogStatus.Text = "Lege mindestens einen Befehl an.";
            return;
        }
        ResultCommands = cleaned;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
