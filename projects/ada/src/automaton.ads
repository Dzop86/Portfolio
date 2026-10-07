--  Every state the controller can reach from Start, with the state after one second and after a request
--  on each axis: the whole automaton, small enough (a few hundred states) for the project page to replay it
--  in the browser, so that the page shows what this Ada code decides and nothing else.
with Ada.Containers.Vectors;
with Traffic; use Traffic;

package Automaton is

   package Controllers is new Ada.Containers.Vectors (Natural, Controller);

   --  The reachable states, Start first, in breadth-first order (so the numbering is stable).
   function Explore (T : Timing) return Controllers.Vector;

   --  Position of C in States (it must be there).
   function Index_Of (States : Controllers.Vector; C : Controller) return Natural;

   --  The states as JSON: phase, elapsed seconds, lights, pending requests, and the indices reached by
   --  Tick and by a request on each axis.
   procedure Put_Json (T : Timing);

end Automaton;
