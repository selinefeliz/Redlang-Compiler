use System;

object TestProgram {
    entry func Main():i {
        declare x:i = 10;
        declare y:i = 20;

        show("--- Simple Test Start ---");
        
        show("x: ");
        show(x);
        show("y: ");
        show(y);
        
        show("Addition (x+y):");
        show(x + y);
        
        show("Check if x < y:");
        check (x < y) {
            show("Yes, x is smaller than y");
        }
        otherwise {
            show("No, x is not smaller than y");
        }
        
        show("Looping 0 to 4:");
        loop (declare j:i = 0; j < 5; set j = j + 1) {
            show(j);
        }
        
        show("--- Simple Test End ---");
        gives 0;
    }
}
