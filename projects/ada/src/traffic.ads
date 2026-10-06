--  Crossroads traffic lights controller. The controller's state is a phase of the cycle and every light is
--  derived from it: a state where both axes are green has no name, so it cannot be written.
package Traffic with SPARK_Mode is

   type Axis is (North_South, East_West);
   type Color is (Red, Yellow, Green);

   type Phase is (NS_Green, NS_Yellow, Red_Before_EW, EW_Green, EW_Yellow, Red_Before_NS);

   function Other (A : Axis) return Axis is (if A = North_South then East_West else North_South);

   --  Colour of axis A during phase P.
   function Light (P : Phase; A : Axis) return Color is
     (case P is
        when NS_Green  => (if A = North_South then Green else Red),
        when NS_Yellow => (if A = North_South then Yellow else Red),
        when EW_Green  => (if A = East_West then Green else Red),
        when EW_Yellow => (if A = East_West then Yellow else Red),
        when Red_Before_EW | Red_Before_NS => Red);

   --  Safety, proved by gnatprove for every phase: at least one axis is red, so the two axes never move
   --  together. Ghost code: it exists for the proof and is not compiled into the program.
   procedure Lemma_One_Axis_Is_Red (P : Phase) with
     Ghost,
     Global => null,
     Post   => Light (P, North_South) = Red or else Light (P, East_West) = Red;

   --  The phase after P; the cycle loops.
   function Next (P : Phase) return Phase is (if P = Phase'Last then Phase'First else Phase'Succ (P));

   subtype Seconds is Natural range 0 .. 3_600;
   subtype Green_Time is Seconds range 10 .. 120;

   Yellow_Time    : constant Seconds := 3;
   All_Red_Time   : constant Seconds := 2;
   Min_Green_Time : constant Green_Time := 10;

   type Timing is record
      NS_Green : Green_Time;
      EW_Green : Green_Time;
   end record;

   type Controller is private;

   function Start (T : Timing) return Controller with
     Post => Current (Start'Result) = NS_Green and Elapsed (Start'Result) = 0;

   function Current (C : Controller) return Phase;
   function Elapsed (C : Controller) return Seconds;

   --  How long phase P lasts in C, a pending request included.
   function Duration_Of (C : Controller; P : Phase) return Seconds;

   --  A car or pedestrian waits on axis A: the green of the other axis ends as soon as it has lasted
   --  Min_Green_Time. A request on the axis that is green changes nothing.
   procedure Request_Crossing (C : in out Controller; A : Axis) with
     Post => Current (C) = Current (C'Old) and Elapsed (C) = Elapsed (C'Old);

   --  One second passes. The phase either stays or moves to the next one, which starts at 0: the cycle
   --  order (green, yellow, all red) is never skipped.
   procedure Tick (C : in out Controller) with
     Post => (if Current (C) = Current (C'Old) then Elapsed (C) = Elapsed (C'Old) + 1
              else Current (C) = Next (Current (C'Old)) and Elapsed (C) = 0);

private

   type Requests is array (Axis) of Boolean;

   type Controller is record
      Timings : Timing  := (NS_Green => Min_Green_Time, EW_Green => Min_Green_Time);
      Phase   : Traffic.Phase := NS_Green;
      Elapsed : Seconds := 0;
      Waiting : Requests := [others => False];
   end record;

   function Current (C : Controller) return Phase is (C.Phase);
   function Elapsed (C : Controller) return Seconds is (C.Elapsed);

end Traffic;
