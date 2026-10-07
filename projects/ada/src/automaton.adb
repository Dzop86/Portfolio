with Ada.Strings.Fixed; use Ada.Strings.Fixed;
with Ada.Text_IO;       use Ada.Text_IO;

package body Automaton is

   use Controllers;

   function Explore (T : Timing) return Vector is
      States : Vector;
      I      : Natural := 0;

      procedure Visit (C : Controller) is
      begin
         if Find_Index (States, C) = No_Index then
            States.Append (C);
         end if;
      end Visit;
   begin
      Visit (Start (T));
      while I <= States.Last_Index loop
         declare
            After_Tick : Controller := States (I);
            NS_Request : Controller := States (I);
            EW_Request : Controller := States (I);
         begin
            Tick (After_Tick);
            Request_Crossing (NS_Request, North_South);
            Request_Crossing (EW_Request, East_West);
            Visit (After_Tick);
            Visit (NS_Request);
            Visit (EW_Request);
         end;
         I := I + 1;
      end loop;
      return States;
   end Explore;

   function Index_Of (States : Vector; C : Controller) return Natural is
      I : constant Extended_Index := Find_Index (States, C);
   begin
      if I = No_Index then
         raise Program_Error with "state outside the automaton";
      end if;
      return I;
   end Index_Of;

   procedure Put_Json (T : Timing) is
      Letter : constant array (Color) of Character := [Red => 'R', Yellow => 'Y', Green => 'G'];
      States : constant Vector := Explore (T);

      function Img (N : Integer) return String is (Trim (N'Image, Ada.Strings.Left));
      function Img (B : Boolean) return String is (if B then "true" else "false");
      function Next_Of (C : Controller; A : Axis) return Natural is
         R : Controller := C;
      begin
         Request_Crossing (R, A);
         return Index_Of (States, R);
      end Next_Of;
   begin
      Put_Line ("{");
      Put_Line ("  ""timings"": { ""ns_green"": " & Img (T.NS_Green) & ", ""ew_green"": " & Img (T.EW_Green)
                & ", ""yellow"": " & Img (Yellow_Time) & ", ""all_red"": " & Img (All_Red_Time)
                & ", ""min_green"": " & Img (Min_Green_Time) & " },");
      Put_Line ("  ""states"": [");
      for I in States.First_Index .. States.Last_Index loop
         declare
            C    : constant Controller := States (I);
            Tick_C : Controller := C;
         begin
            Tick (Tick_C);
            Put ("    { ""phase"": """ & Phase'Image (Current (C)) & """, ""elapsed"": " & Img (Elapsed (C))
                 & ", ""lights"": """ & Letter (Light (Current (C), North_South))
                 & Letter (Light (Current (C), East_West)) & """"
                 & ", ""pending"": [" & Img (Pending (C, North_South)) & ", " & Img (Pending (C, East_West)) & "]"
                 & ", ""tick"": " & Img (Index_Of (States, Tick_C))
                 & ", ""request"": [" & Img (Next_Of (C, North_South)) & ", " & Img (Next_Of (C, East_West)) & "] }");
            Put_Line (if I = States.Last_Index then "" else ",");
         end;
      end loop;
      Put_Line ("  ]");
      Put_Line ("}");
   end Put_Json;

end Automaton;
