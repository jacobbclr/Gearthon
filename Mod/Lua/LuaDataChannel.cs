using System;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using MoonSharp.Interpreter;
using static SmashHammer.GearBlocks.Construction.PartBehaviourBase;

namespace Gearthon.Lua;

class LuaDataChannel : Il2CppSystem.Object
{
    ScriptBehaviour script;
    
    static LuaDataChannel()
    {
        ClassInjector.RegisterTypeInIl2Cpp<LuaDataChannel>();
        UserData.RegisterType<LuaDataChannel>();
    }

    public LuaDataChannel(IntPtr ptr) : base(ptr) { }
    public LuaDataChannel(ScriptBehaviour script) : base(ClassInjector.DerivedConstructorPointer<LuaDataChannel>())
    {
        ClassInjector.DerivedConstructorBody(this);
        this.script = script;
    }

    public LuaDataChannelValue Create(string label, int decimal_places)
    {
        DataChannel channel = script.CreateDataChannel(label, $"{label}: {{0:F{decimal_places}}}", $"{label}: {{0:F{decimal_places}}}");
        return new LuaDataChannelValue(channel);
    }
}