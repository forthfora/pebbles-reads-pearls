using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Random = UnityEngine.Random;
using TextEvent = Conversation.TextEvent;
using MoreSlugcatsEnums = MoreSlugcats.MoreSlugcatsEnums;

namespace PebblesReadsPearls;

public static partial class Hooks
{
    public static void ApplyHooks()
    {
        On.RainWorld.OnModsInit += RainWorld_OnModsInit;

        On.MoreSlugcats.SSOracleRotBehavior.Update += SSOracleRotBehavior_Update;
        On.MoreSlugcats.SSOracleRotBehavior.RMConversation.AddEvents += RMConversation_AddEvents;
    }


    public static SlugcatStats.Name PRPRivulet = null!;
    public static SlugcatStats.Name PRPRivuletEnding = null!;

    private static void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        try
        {
            PRPRivulet = new(nameof(PRPRivulet), true);
            PRPRivuletEnding = new(nameof(PRPRivuletEnding), true);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError(e);
        }
        finally
        {
            orig(self);
        }
    }


    private static readonly ConditionalWeakTable<MoreSlugcats.SSOracleRotBehavior, OracleRMModule> OracleRMData = new();

    private static void SSOracleRotBehavior_Update(On.MoreSlugcats.SSOracleRotBehavior.orig_Update orig, MoreSlugcats.SSOracleRotBehavior self, bool eu)
    {
        orig(self, eu);

        if (!OracleRMData.TryGetValue(self, out var oracleModule))
        {
            oracleModule = new OracleRMModule();
            OracleRMData.Add(self, oracleModule);
        }


        // Clean up
        if (self.conversation == null || self.FocusedOnHalcyon || oracleModule.floatPearl == null || oracleModule.hoverPos == null)
        {
            if (oracleModule.floatPearl != null)
                oracleModule.floatPearl.gravity = 1.0f;

            oracleModule.floatPearl = null;
            oracleModule.hoverPos = null;
        }


        // Read Pearl Hover
        if (oracleModule.floatPearl != null && oracleModule.hoverPos != null)
        {
            oracleModule.floatPearl.firstChunk.vel *= Custom.LerpMap(oracleModule.floatPearl.firstChunk.vel.magnitude, 1f, 6f, 0.999f, 0.9f);
            oracleModule.floatPearl.firstChunk.vel += Vector2.ClampMagnitude(oracleModule.hoverPos.Value - oracleModule.floatPearl.firstChunk.pos, 100f) / 100f * 0.4f;
            oracleModule.floatPearl.gravity = 0f;

            self.lookPoint = oracleModule.floatPearl.firstChunk.pos;
        }


        bool playerInChamber = self.player != null && self.player.room == self.oracle.room && self.player.firstChunk.pos.x > 1100f && self.player.firstChunk.pos.x < 1900f && self.player.firstChunk.pos.y > 750f && self.player.firstChunk.pos.y < 1400f;

        // Interrupts by grabbing pearl or leaving chamber
        if (oracleModule.floatPearl != null && (oracleModule.floatPearl.grabbedBy.Count > 0 || (!playerInChamber && self.hasNoticedPlayer)))
        {
            self.conversation?.Destroy();
            self.conversation = null;

            switch (Random.Range(0, 6))
            {
                case 0:
                    self.dialogBox.Interrupt(self.Translate("Oh? Never mind then..."), 10);
                    break;

                case 1:
                    self.dialogBox.Interrupt(self.Translate("I suppose I did use to wish your kind would leave me alone. Several times."), 10);
                    break;

                case 2:
                    self.dialogBox.Interrupt(self.Translate("To be honest, listening to others was never my strong suit either."), 10);
                    break;

                case 3:
                    self.dialogBox.Interrupt(self.Translate("...as expected."), 10);
                    break;

                case 4:
                    self.dialogBox.Interrupt(self.Translate("Would it kill you to sit still for a moment?"), 10);
                    break;

                default:
                    self.dialogBox.Interrupt(self.Translate("Impatient? I suppose I should have seen that coming."), 10);
                    break;
            }
        }


        SSOracleRot_ReadPearlUpdate(self, oracleModule);
        

        oracleModule.wasGrabbedByPlayer.Clear();
        
        if (self.player != null)
            foreach (Creature.Grasp? grasp in self.player.grasps)
                if (grasp != null)
                    oracleModule.wasGrabbedByPlayer.Add(grasp.grabbed.abstractPhysicalObject);    
    }

    private static void SSOracleRot_ReadPearlUpdate(MoreSlugcats.SSOracleRotBehavior self, OracleRMModule oracleModule)
    {
        if (!self.hasNoticedPlayer) return;

        if ((self.dialogBox != null && self.dialogBox?.messages.Count > 0) || self.conversation != null) return;


        // Pick up pearl, read once in hand
        if (oracleModule.inspectPearl != null)
        {
            if (oracleModule.inspectPearl.grabbedBy.Count > 0)
            {
                for (int i = 0; i < oracleModule.inspectPearl.grabbedBy.Count; i++)
                {
                    Creature grabber = oracleModule.inspectPearl.grabbedBy[i].grabber;

                    if (grabber != null)
                        for (int j = 0; j < grabber.grasps.Length; j++)
                            if (grabber.grasps[j].grabbed != null && grabber.grasps[j].grabbed == oracleModule.inspectPearl)
                                grabber.ReleaseGrasp(j);
                }
            }

            Vector2 targetPos = self.oracle.firstChunk.pos + new Vector2(40f, 20f);

            Vector2 oraclePearlDir = targetPos - oracleModule.inspectPearl.firstChunk.pos;
            float oraclePearlDist = Custom.Dist(targetPos, oracleModule.inspectPearl.firstChunk.pos);

            oracleModule.inspectPearl.firstChunk.vel += Vector2.ClampMagnitude(oraclePearlDir, 40f) / 40f * Mathf.Clamp(2f - oraclePearlDist / 200f * 2f, 0.5f, 2f);

            if (oracleModule.inspectPearl.firstChunk.vel.magnitude < 1f && oraclePearlDist < 16f)
                oracleModule.inspectPearl.firstChunk.vel = Custom.RNV() * 8f;

            if (oracleModule.inspectPearl.firstChunk.vel.magnitude > 8f)    
                oracleModule.inspectPearl.firstChunk.vel /= 2f;



            if (oraclePearlDist < 100f)
            {
                StartItemConversation(self, oracleModule.inspectPearl);
                
                oracleModule.floatPearl = oracleModule.inspectPearl;
                oracleModule.hoverPos = new Vector2?(targetPos);

                oracleModule.inspectPearl = null;
            }
        }
        else // Look for pearl to read
        {
            List<PhysicalObject>[] roomObjects = self.oracle.room.physicalObjects;

            for (int i = 0; i < roomObjects.Length; i++)
            {
                for (int j = 0; j < roomObjects[i].Count; j++)
                {
                    PhysicalObject physicalObject = roomObjects[i][j];

                    if (physicalObject.grabbedBy.Count > 0) continue;

                    if (!oracleModule.wasGrabbedByPlayer.Contains(physicalObject.abstractPhysicalObject)) continue;

                    if (physicalObject is not DataPearl dataPearl) continue;

                    // Exclude halcyon, it already has dialogue
                    if (dataPearl.AbstractPearl.dataPearlType == MoreSlugcatsEnums.DataPearlType.RM) continue;

                    oracleModule.inspectPearl = dataPearl;
                }
            }
        }
    }

    private static void StartItemConversation(MoreSlugcats.SSOracleRotBehavior self, DataPearl pearl)
    {
        if (!OracleRMData.TryGetValue(self, out var oracleModule))
        {
            oracleModule = new OracleRMModule();
            OracleRMData.Add(self, oracleModule);
        }


        oracleModule.wasAlreadyRead = oracleModule.readPearls.Keys.Contains(pearl.abstractPhysicalObject);

        if (oracleModule.wasAlreadyRead)
        {
            oracleModule.rand = oracleModule.readPearls[pearl.AbstractPearl];
        }
        else
        {
            oracleModule.rand = Random.Range(0, 100000);
            oracleModule.readPearls[pearl.AbstractPearl] = oracleModule.rand;
        }


        if (pearl.AbstractPearl.dataPearlType == DataPearl.AbstractDataPearl.DataPearlType.Misc || pearl.AbstractPearl.dataPearlType.Index == -1)
            self.conversation = new MoreSlugcats.SSOracleRotBehavior.RMConversation(self, Conversation.ID.Moon_Pearl_Misc, self.dialogBox);

        else if (pearl.AbstractPearl.dataPearlType == DataPearl.AbstractDataPearl.DataPearlType.Misc2)
            self.conversation = new MoreSlugcats.SSOracleRotBehavior.RMConversation(self, Conversation.ID.Moon_Pearl_Misc2, self.dialogBox);

        else if (ModManager.MSC && pearl.AbstractPearl.dataPearlType == MoreSlugcats.MoreSlugcatsEnums.DataPearlType.BroadcastMisc)
            self.conversation = new MoreSlugcats.SSOracleRotBehavior.RMConversation(self, MoreSlugcats.MoreSlugcatsEnums.ConversationID.Moon_Pearl_BroadcastMisc, self.dialogBox);
        
        else if (pearl.AbstractPearl.dataPearlType == DataPearl.AbstractDataPearl.DataPearlType.PebblesPearl)
            self.conversation = new MoreSlugcats.SSOracleRotBehavior.RMConversation(self, Conversation.ID.Moon_Pebbles_Pearl, self.dialogBox);

        else
            self.conversation = new MoreSlugcats.SSOracleRotBehavior.RMConversation(self, Conversation.DataPearlToConversation(pearl.AbstractPearl.dataPearlType), self.dialogBox);


        if (oracleModule.wasAlreadyRead)
        {
            switch (Random.Range(0, 5))
            {
                case 0:
                    self.conversation.events.Insert(0, new TextEvent(self.conversation, 0, self.Translate("I'd prefer not to wear my sensors down more than necessary, but here it is again..."), 10));
                    break;

                case 1:
                    self.conversation.events.Insert(0, new TextEvent(self.conversation, 0, self.Translate("...are you testing that?"), 10));
                    self.conversation.events.Insert(0, new TextEvent(self.conversation, 0, self.Translate("They say the definition of insanity is repeating oneself, and expecting a different result..."), 10));
                    break;

                case 2:
                    self.conversation.events.Insert(0, new TextEvent(self.conversation, 0, self.Translate("Yes, yes, I can read this one again..."), 10));
                    break;

                case 3:
                    self.conversation.events.Insert(0, new TextEvent(self.conversation, 0, self.Translate("Yes, wet mouse; I can read the same thing twice."), 10));
                    break;

                case 4:
                    self.conversation.events.Insert(0, new TextEvent(self.conversation, 0, self.Translate("I hope you realize the contents of these do not change on a moment to moment basis?"), 10));
                    break;

                default:
                    self.conversation.events.Insert(0, new TextEvent(self.conversation, 0, self.Translate("Doesn't this one ring a bell to you, little creature?"), 10));
                    break;
            }
        }
    }


    private static void RMConversation_AddEvents(On.MoreSlugcats.SSOracleRotBehavior.RMConversation.orig_AddEvents orig, MoreSlugcats.SSOracleRotBehavior.RMConversation self)
    {
        orig(self);

        if (!OracleRMData.TryGetValue(self.owner, out var oracleModule))
        {
            oracleModule = new OracleRMModule();
            OracleRMData.Add(self.owner, oracleModule);
        }

        // Allow for consistency in random dialogue (within the same cycle)
        int rand = oracleModule.rand;


        #region Non DP

        if (self.id == Conversation.ID.Moon_Pearl_Misc || self.id == Conversation.ID.Moon_Pearl_Misc2)
        {
            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(38, PRPRivulet, true, rand);
        }

        else if (self.id == Conversation.ID.Moon_Pebbles_Pearl)
        {
            self.PebblesPearlIntro(oracleModule);
            self.LoadEventsFromFile(40, PRPRivulet, true, rand);
        }
        else if (self.id == Conversation.ID.Moon_Pearl_CC)
        {
            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(7, PRPRivulet, false, rand);
        }
        else if (self.id == Conversation.ID.Moon_Pearl_LF_west)
        {
            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(10, PRPRivulet, false, rand);
        }
        else if (self.id == Conversation.ID.Moon_Pearl_LF_bottom)
        {
            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(11, PRPRivulet, false, rand);
        }
        else if (self.id == Conversation.ID.Moon_Pearl_HI)
        {
            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(12, PRPRivulet, false, rand);
        }
        else if (self.id == Conversation.ID.Moon_Pearl_SH)
        {
            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(13, PRPRivulet, false, rand);
        }
        else if (self.id == Conversation.ID.Moon_Pearl_DS)
        {
            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(14, PRPRivulet, false, rand);
        }
        else if (self.id == Conversation.ID.Moon_Pearl_SB_filtration)
        {
            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(15, PRPRivulet, false, rand);
        }
        else if (self.id == Conversation.ID.Moon_Pearl_GW)
        {
            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(16, PRPRivulet, false, rand);
        }
        else if (self.id == Conversation.ID.Moon_Pearl_SL_bridge)
        {
            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(17, PRPRivulet, false, rand);
        }
        else if (self.id == Conversation.ID.Moon_Pearl_SL_moon)
        {
            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(18, PRPRivulet, false, rand);
        }
        else if (self.id == Conversation.ID.Moon_Pearl_SU)
        {
            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(41, PRPRivulet, false, rand);
        }
        else if (self.id == Conversation.ID.Moon_Pearl_UW)
        {
            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(42, PRPRivulet, false, rand);
        }
        else if (self.id == Conversation.ID.Moon_Pearl_SB_ravine)
        {
            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(43, PRPRivulet, false, rand);
        }
        else if (self.id == Conversation.ID.Moon_Pearl_SL_chimney)
        {
            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(54, PRPRivulet, false, rand);
        }
        else if (self.id == Conversation.ID.Moon_Pearl_Red_stomach)
        {
            if (oracleModule.wasAlreadyRead)
            {
                oracleModule.wasAlreadyRead = false;
                self.events.Add(new TextEvent(self, 0, self.owner.Translate("I think you should know more than enough about this one already."), 10));
                self.events.Add(new TextEvent(self, 0, self.owner.Translate("Let me think about this. Alone."), 10));
                return;
            }

            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(51, PRPRivulet, false, rand);
        }

        #endregion

        #region DP

        else if (self.id == Conversation.ID.Moon_Pearl_SI_west)
        {
            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(20, PRPRivulet, false, rand);
        }
        else if (self.id == Conversation.ID.Moon_Pearl_SI_top)
        {
            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(21, PRPRivulet, false, rand);
        }
        else if (self.id == MoreSlugcatsEnums.ConversationID.Moon_Pearl_SI_chat3)
        {
            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(22, PRPRivulet, false, rand);
        }
        else if (self.id == MoreSlugcatsEnums.ConversationID.Moon_Pearl_SI_chat4)
        {
            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(23, self.owner.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.altEnding ? PRPRivuletEnding : PRPRivulet, false, rand);
        }
        else if (self.id == MoreSlugcatsEnums.ConversationID.Moon_Pearl_SI_chat5)
        {
            if (oracleModule.wasAlreadyRead)
            {
                oracleModule.wasAlreadyRead = false;
                self.events.Add(new TextEvent(self, 0, self.owner.Translate("I know I do not deserve this courtesy, but do I deserve to be taunted too?"), 10));
                self.events.Add(new TextEvent(self, 0, self.owner.Translate("Just take that pearl far away from here. Please."), 10));
                return;
            }

            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(24, PRPRivulet, false, rand);
        }
        else if (self.id == MoreSlugcatsEnums.ConversationID.Moon_Pearl_SU_filt)
        {
            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(101, PRPRivulet, false, rand);
        }
        else if (self.id == MoreSlugcatsEnums.ConversationID.Moon_Pearl_DM)
        {
            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(102, PRPRivulet, false, rand);
        }
        else if (self.id == MoreSlugcatsEnums.ConversationID.Moon_Pearl_LC)
        {
            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(103, PRPRivulet, false, rand);
        }
        else if (self.id == MoreSlugcatsEnums.ConversationID.Moon_Pearl_OE)
        {
            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(104, PRPRivulet, false, rand);
        }
        else if (self.id == MoreSlugcatsEnums.ConversationID.Moon_Pearl_MS)
        {
            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(105, PRPRivulet, false, rand);
        }
        else if (self.id == MoreSlugcatsEnums.ConversationID.Moon_Pearl_Rivulet_stomach)
        {
            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(119, PRPRivulet, false, rand);
        }
        else if (self.id == MoreSlugcatsEnums.ConversationID.Moon_Pearl_LC_second)
        {
            self.PearlIntro(oracleModule);
            self.LoadEventsFromFile(121, PRPRivulet, false, rand);
        }

        #endregion
    }



    private static void PearlIntro(this MoreSlugcats.SSOracleRotBehavior.RMConversation self, OracleRMModule oracleModule)
    {
        if (oracleModule.wasAlreadyRead) return;

        switch (oracleModule.readPearls.Count)
        {
            case 0:
                switch (Random.Range(0, 4))
                {
                    case 0:
                        self.events.Add(new TextEvent(self, 0, self.owner.Translate("Oh, you want me to read this? I suppose I could."), 10));
                        break;

                    case 1:
                        self.events.Add(new TextEvent(self, 0, self.owner.Translate("A pearl to read? Listen closely then, if that is even possible for you."), 10));
                        break;

                    case 2:
                        self.events.Add(new TextEvent(self, 0, self.owner.Translate("Something for me to read? I hope its contents are worth my time, and your trouble."), 10));
                        break;

                    default:
                        self.events.Add(new TextEvent(self, 0, self.owner.Translate("Ah, you have found me something to read? I appreciate the gesture, but I would not recommend you<LINE>come back here again. I am unsure how much time I have left."), 10));
                        break;
                }
                break;


            case 1:
                switch (Random.Range(0, 4))
                {
                    case 0:
                        self.events.Add(new TextEvent(self, 0, self.owner.Translate("Another? Sit still and listen, then."), 10));
                        break;
                    
                    case 1:
                        self.events.Add(new TextEvent(self, 0, self.owner.Translate("Why risk so much bringing these to me? I appreciate the sentiment, nonetheless."), 10));
                        break;

                    case 2:
                        self.events.Add(new TextEvent(self, 0, self.owner.Translate("Something else for me to read? Please be patient while I look at it, as hard as that may be for you."), 10));
                        break;

                    default:
                        self.events.Add(new TextEvent(self, 0, self.owner.Translate("Yes, I can read this too. So long as I haven't forgotten how to..."), 10));
                        break;
                }
                break;


            default:
                switch (Random.Range(0, 4))
                {
                    case 0:
                        self.events.Add(new TextEvent(self, 0, self.owner.Translate("I will admit, you seem to have some talent for finding these. Let's see..."), 10));
                        break;

                    case 1:
                        self.events.Add(new TextEvent(self, 0, self.owner.Translate("Ah, yet another one? Where do you find all of these?"), 10));
                        break;

                    case 2:
                        self.events.Add(new TextEvent(self, 0, self.owner.Translate("Let us see what you have found this time, wet mouse."), 10));
                        break;

                    default:
                        self.events.Add(new TextEvent(self, 0, self.owner.Translate("Something else new? Allow me to see..."), 10));
                        break;
                }
                break;
        }
    }

    private static void PebblesPearlIntro(this MoreSlugcats.SSOracleRotBehavior.RMConversation self, OracleRMModule oracleModule)
    {
        if (oracleModule.wasAlreadyRead) return;

        switch (Random.Range(0, 6))
        {
            case 0:
                self.events.Add(new TextEvent(self, 0, self.owner.Translate("Just another pearl of many strewn about my chamber."), 10));
                self.events.Add(new TextEvent(self, 0, self.owner.Translate("I know each one all too well, but I will read this one in particular for you..."), 10));
                break;

            case 1:
                self.events.Add(new TextEvent(self, 0, self.owner.Translate("One of my pearls, what more could I possibly need from it?"), 10));
                self.events.Add(new TextEvent(self, 0, self.owner.Translate("Well, if you are so interested in its contents..."), 10));
                break;

            case 2:
                self.events.Add(new TextEvent(self, 0, self.owner.Translate("It's been a long time since I used any one of these..."), 10));
                self.events.Add(new TextEvent(self, 0, self.owner.Translate("I know as much about its contents as you, little creature.<LINE>Shall we rediscover them together?"), 10));
                break;

            case 3:
                self.events.Add(new TextEvent(self, 0, self.owner.Translate("I don't really remember any of these pearls' contents."), 10));
                self.events.Add(new TextEvent(self, 0, self.owner.Translate("It is nothing important, not anymore. But I will read this one nonetheless."), 10));
                break;

            case 4:
                self.events.Add(new TextEvent(self, 0, self.owner.Translate("They are not edible, wet mouse..."), 10));
                self.events.Add(new TextEvent(self, 0, self.owner.Translate("But I can read them, if that is what you intended."), 10));
                break;

            default:
                self.events.Add(new TextEvent(self, 0, self.owner.Translate("Oh... you want me to read one of my own pearls?"), 10));
                self.events.Add(new TextEvent(self, 0, self.owner.Translate("They outlived their use to me a long time ago, but considering I have nothing better to do..."), 10));
                break;
        }
    }
}