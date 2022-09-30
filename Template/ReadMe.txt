Issusing commands
======================
 MQ.Cmd("/bccmd connect");


 Querying TLOs
 =====================

String TargetType = MQ.Query<String>($"${{Spell[{CastName}].TargetType}}");
Int32 Duration = MQ.Query<Int32>($"${{Spell[{CastName}].Duration}}");
Decimal RecastTime = MQ.Query<Decimal>($"${{Spell[{CastName}].RecastTime}}");
Decimal RecoveryTime = MQ.Query<Decimal>($"${{Spell[{CastName}].RecoveryTime}}");
Decimal MyCastTime = MQ.Query<Decimal>($"${{Spell[{CastName}].MyCastTime}}");

if (!MQ.Query<bool>("${Plugin[MQ2EQBC]}"))
{                

}

Using Delay
===============

MQ.Delay(1000); //delay 1 sec

//wait 300 milliseconds until we are still, basically works like the macro delay
MQ.Delay(300, "${Bool[!${Me.Moving}]}");


Writing to MQ console
===============

MQ.Write("Hi there..");

MQ.Write("\arHi There..") //hi there in red.


Writing to MQ console via _log
===============

_log.Write("Hi there!");//writes to whatever the default log is set to.
_log.Write("ERROR OMG!", LogLevel.Error); //will always write to error even if the default is different.

_log.Write has class/method/line# included and can be turned on/off dynamically. MQ.Write cannot.

Registering an event
================================================
//Rekken tells the group, 'HI THERE!'
 EventProcessor.RegisterEvent("EverythingEvent", "(.+) tells the group, '(.+)'", (x) => {
                
                _log.Write($"{ x.eventName}:Processed:{ x.eventString}");

});

//Rekken tells the group, 'nowCast Dawnstrike targetid=88'

List<String> r = new List<string>();
r.Add("(.+) tells the group, 'nowCast (.+) targetid=(.+)'");
r.Add("(.+) tells the says, 'nowCast (.+) targetid=(.+)'");
EventProcessor.RegisterEvent("nowCastEvent", r, (x) => {
    _log.Write($"Processing {x.eventName}");
                
    string user = string.Empty;
    string spellName = String.Empty;
    Int32 targetid = 0;
    if (x.match.Groups.Count > 3)
    {
        user = x.match.Groups[1].Value;
        spellName = x.match.Groups[2].Value;
        Int32.TryParse(x.match.Groups[3].Value, out targetid);

    }
    _log.Write($"{ x.eventName}:{ user} asked to cast the spell:{spellName}");

    Data.Spell spell = new Data.Spell(spellName);
    CastReturn returnValue = Cast(targetid, spell);

    _log.Write($"{ x.eventName}: {spellName} result?: {returnValue.ToString()}");

});

Using Trace
=================
using(_log.Trace())
{

}
using(_log.Trace("TraceName"))
{

}

Color Codes for MQ.Write and _Log
==============
 
WriteChatColor("\ayYELLOW    \a-yDARK YELLOW");
WriteChatColor("\aoORANGE    \a-oDARK ORANGE");
WriteChatColor("\agGREEN     \a-gDARK GREEN");
WriteChatColor("\auBLUE      \a-uDARK BLUE");
WriteChatColor("\arRED       \a-rDARK RED");
WriteChatColor("\atTEAL      \a-tDARK TEAL");
WriteChatColor("\abBLACK");
WriteChatColor("\amMAGENTA   \a-mDARK MAGENTA");
WriteChatColor("\apPURPLE    \a-pDARK PURPLE");
WriteChatColor("\awWHITE     \a-wGREY");

example: "\auTHIS IS BLUE \apAND THIS IS PURPLE"
                 