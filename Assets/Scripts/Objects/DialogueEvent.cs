using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class DialogueEvent
{
    public string Text { get; private set; }
    public Action Event;

    public (string, Action) GetValues() => (Text, Event);

    public DialogueEvent(string text, Action action)
    {
        Text = text;
        Event = action;
    }
}