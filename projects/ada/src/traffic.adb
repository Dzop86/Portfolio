package body Traffic with SPARK_Mode is

   function Light (P : Phase; A : Axis) return Color is
     (case P is
        when NS_Green  => (if A = North_South then Green else Red),
        when NS_Yellow => (if A = North_South then Yellow else Red),
        when EW_Green  => (if A = East_West then Green else Red),
        when EW_Yellow => (if A = East_West then Yellow else Red),
        when Red_Before_EW | Red_Before_NS => Red);

   function Start (T : Timing) return Controller is
     ((Timings => T, Phase => NS_Green, Elapsed => 0, Waiting => [others => False]));

   function Duration_Of (C : Controller; P : Phase) return Seconds is
     (case P is
        when NS_Green => (if C.Waiting (East_West) then Min_Green_Time else C.Timings.NS_Green),
        when EW_Green => (if C.Waiting (North_South) then Min_Green_Time else C.Timings.EW_Green),
        when NS_Yellow | EW_Yellow => Yellow_Time,
        when Red_Before_EW | Red_Before_NS => All_Red_Time);

   procedure Request_Crossing (C : in out Controller; A : Axis) is
   begin
      if Light (C.Phase, A) /= Green then
         C.Waiting (A) := True;
      end if;
   end Request_Crossing;

   procedure Tick (C : in out Controller) is
   begin
      if C.Elapsed + 1 >= Duration_Of (C, C.Phase) then
         C.Phase := Next (C.Phase);
         C.Elapsed := 0;
         --  The axis that now turns green has been served.
         case C.Phase is
            when NS_Green => C.Waiting (North_South) := False;
            when EW_Green => C.Waiting (East_West) := False;
            when others   => null;
         end case;
      else
         C.Elapsed := C.Elapsed + 1;
      end if;
   end Tick;

end Traffic;
