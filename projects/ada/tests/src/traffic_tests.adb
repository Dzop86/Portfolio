with AUnit.Assertions; use AUnit.Assertions;
with Automaton;
with Traffic;          use Traffic;

package body Traffic_Tests is

   use AUnit.Test_Cases.Registration;

   Default : constant Timing := (NS_Green => 30, EW_Green => 20);

   procedure Run (C : in out Controller; Seconds_To_Run : Natural) is
   begin
      for I in 1 .. Seconds_To_Run loop
         Tick (C);
      end loop;
   end Run;

   --  Every phase gives each axis a colour; no phase lets both axes move.
   procedure Test_No_Phase_Lets_Both_Axes_Move (T : in out AUnit.Test_Cases.Test_Case'Class) is
      pragma Unreferenced (T);
   begin
      for P in Phase loop
         Assert (Light (P, North_South) = Red or else Light (P, East_West) = Red,
                 "phase " & Phase'Image (P) & " lets both axes move");
      end loop;
      Assert (Light (NS_Green, North_South) = Green and Light (NS_Green, East_West) = Red, "NS_Green");
      Assert (Light (EW_Yellow, East_West) = Yellow and Light (EW_Yellow, North_South) = Red, "EW_Yellow");
      Assert (Light (Red_Before_EW, North_South) = Red and Light (Red_Before_EW, East_West) = Red, "all red");
   end Test_No_Phase_Lets_Both_Axes_Move;

   --  A full cycle lasts green + yellow + all red on each axis, then starts again.
   procedure Test_A_Full_Cycle_Comes_Back_To_The_Start (T : in out AUnit.Test_Cases.Test_Case'Class) is
      pragma Unreferenced (T);
      C : Controller := Start (Default);
      Cycle : constant Natural := 30 + 20 + 2 * (Yellow_Time + All_Red_Time);
   begin
      Assert (Current (C) = NS_Green, "starts with north-south green");
      Run (C, 30);
      Assert (Current (C) = NS_Yellow, "yellow after 30 s of green");
      Run (C, Yellow_Time);
      Assert (Current (C) = Red_Before_EW, "all red after yellow");
      Run (C, Cycle - 30 - Yellow_Time);
      Assert (Current (C) = NS_Green and Elapsed (C) = 0, "back to the start after one cycle");
   end Test_A_Full_Cycle_Comes_Back_To_The_Start;

   --  Over a long run with requests, phases only move forward in the cycle and yellow always lasts
   --  Yellow_Time, then gives way to all red.
   procedure Test_Yellow_Is_Always_Followed_By_All_Red (T : in out AUnit.Test_Cases.Test_Case'Class) is
      pragma Unreferenced (T);
      C : Controller := Start (Default);
      Before : Phase;
      Yellow_Run : Natural := 0;
   begin
      for Second in 1 .. 2_000 loop
         if Second mod 37 = 0 then
            Request_Crossing (C, East_West);
         elsif Second mod 53 = 0 then
            Request_Crossing (C, North_South);
         end if;
         Before := Current (C);
         Tick (C);
         Assert (Current (C) = Before or else Current (C) = Next (Before), "phases move forward one at a time");
         if Before in NS_Yellow | EW_Yellow then
            Yellow_Run := Yellow_Run + 1;
            if Current (C) /= Before then
               Assert (Current (C) in Red_Before_EW | Red_Before_NS, "yellow is followed by all red");
               Assert (Yellow_Run = Yellow_Time, "yellow lasts exactly Yellow_Time");
               Yellow_Run := 0;
            end if;
         end if;
      end loop;
   end Test_Yellow_Is_Always_Followed_By_All_Red;

   --  A request on the waiting axis cuts the other green short, but never below the minimum green time.
   procedure Test_A_Request_Shortens_The_Other_Green (T : in out AUnit.Test_Cases.Test_Case'Class) is
      pragma Unreferenced (T);
      C : Controller := Start ((NS_Green => 90, EW_Green => 20));
   begin
      Run (C, 4);
      Request_Crossing (C, East_West);
      Run (C, Min_Green_Time - 4 - 1);
      Assert (Current (C) = NS_Green, "green is kept for at least Min_Green_Time");
      Run (C, 1);
      Assert (Current (C) = NS_Yellow, "green ends at Min_Green_Time after a request");

      C := Start ((NS_Green => 90, EW_Green => 20));
      Run (C, 50);
      Request_Crossing (C, East_West);
      Tick (C);
      Assert (Current (C) = NS_Yellow, "a late request ends the green at once");
   end Test_A_Request_Shortens_The_Other_Green;

   procedure Test_A_Request_On_The_Green_Axis_Changes_Nothing (T : in out AUnit.Test_Cases.Test_Case'Class) is
      pragma Unreferenced (T);
      C : Controller := Start (Default);
   begin
      Run (C, 5);
      Request_Crossing (C, North_South);
      Run (C, 24);
      Assert (Current (C) = NS_Green, "north-south keeps its full green");
      Run (C, 1);
      Assert (Current (C) = NS_Yellow, "then turns yellow on time");
   end Test_A_Request_On_The_Green_Axis_Changes_Nothing;

   --  The request is consumed when the requesting axis gets its green.
   procedure Test_A_Request_Is_Served_Once (T : in out AUnit.Test_Cases.Test_Case'Class) is
      pragma Unreferenced (T);
      C : Controller := Start (Default);
   begin
      Request_Crossing (C, East_West);
      Run (C, Min_Green_Time + Yellow_Time + All_Red_Time);
      Assert (Current (C) = EW_Green, "east-west served early");
      Run (C, 20 + Yellow_Time + All_Red_Time);
      Assert (Current (C) = NS_Green, "next cycle");
      Run (C, Min_Green_Time);
      Assert (Current (C) = NS_Green, "the served request no longer shortens north-south");
   end Test_A_Request_Is_Served_Once;

   --  Green times outside 10 .. 120 s cannot even be built. With a literal, GNAT already warns at compile
   --  time; the value is read from a string here so that the run-time check is the one exercised.
   procedure Test_Green_Times_Out_Of_Range_Are_Rejected (T : in out AUnit.Test_Cases.Test_Case'Class) is
      pragma Unreferenced (T);
      Value : constant Natural := Natural'Value ("5");
   begin
      declare
         Bad : constant Green_Time := Green_Time (Value);
         pragma Unreferenced (Bad);
      begin
         Assert (False, "a 5 s green was accepted");  --  raises AUnit's assertion error, not Constraint_Error
      end;
   exception
      when Constraint_Error => null;  --  expected
   end Test_Green_Times_Out_Of_Range_Are_Rejected;

   --  The automaton the project page replays (D46): every reachable state is safe, every transition stays
   --  inside it, and it starts where the controller starts.
   procedure Test_The_Automaton_Is_Closed_And_Safe (T : in out AUnit.Test_Cases.Test_Case'Class) is
      pragma Unreferenced (T);
      use Automaton.Controllers;
      States : constant Vector := Automaton.Explore (Default);
   begin
      Assert (States (0) = Start (Default), "state 0 is Start");
      --  Six phases, at most 30 s in one, two requests: far fewer than 6 * 31 * 4 states.
      Assert (Natural (States.Length) in 10 .. 6 * 31 * 4, "size:" & States.Length'Image);
      for C of States loop
         Assert (Light (Current (C), North_South) = Red or else Light (Current (C), East_West) = Red,
                 "a reachable state lets both axes move");
         declare
            After : Controller := C;
            NS, EW : Controller := C;
         begin
            Tick (After);
            Request_Crossing (NS, North_South);
            Request_Crossing (EW, East_West);
            Assert (Automaton.Index_Of (States, After) <= States.Last_Index, "tick");
            Assert (Automaton.Index_Of (States, NS) <= States.Last_Index, "request north-south");
            Assert (Automaton.Index_Of (States, EW) <= States.Last_Index, "request east-west");
         end;
      end loop;
   end Test_The_Automaton_Is_Closed_And_Safe;

   overriding procedure Register_Tests (T : in out Test) is
   begin
      Register_Routine (T, Test_No_Phase_Lets_Both_Axes_Move'Access, "no phase lets both axes move");
      Register_Routine (T, Test_A_Full_Cycle_Comes_Back_To_The_Start'Access, "a full cycle comes back to the start");
      Register_Routine (T, Test_Yellow_Is_Always_Followed_By_All_Red'Access, "yellow is always followed by all red");
      Register_Routine (T, Test_A_Request_Shortens_The_Other_Green'Access, "a request shortens the other green");
      Register_Routine (T, Test_A_Request_On_The_Green_Axis_Changes_Nothing'Access, "a request on the green axis changes nothing");
      Register_Routine (T, Test_A_Request_Is_Served_Once'Access, "a request is served once");
      Register_Routine (T, Test_Green_Times_Out_Of_Range_Are_Rejected'Access, "green times out of range are rejected");
      Register_Routine (T, Test_The_Automaton_Is_Closed_And_Safe'Access, "the automaton is closed and safe");
   end Register_Tests;

   overriding function Name (T : Test) return AUnit.Message_String is
      pragma Unreferenced (T);
   begin
      return AUnit.Format ("Traffic");
   end Name;

end Traffic_Tests;
