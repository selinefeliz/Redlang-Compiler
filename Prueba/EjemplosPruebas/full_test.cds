use System;
use Generics;

object Entity {
    declare id:i;
    declare name:s;
    declare active:b = true;

    func Setup(newId:i, newName:s):void {
        set id = newId;
        set name = newName;
    }

    func Display():void {
        show("Entity [");
        show(id);
        show("]: ");
        show(name);
        show("\n");
    }
}

object Calculator {
    func Multiply(val1:f, val2:f):f {
        gives val1 * val2;
    }

    func GetModulo(arg1:i, arg2:i):i {
        gives arg1 % arg2;
    }
}

object TestProgram {
    entry func Main():i {
        declare myIntExtra:i = 42;
        declare myFloatExtra:f = 3.14159;
        declare myBoolExtra:b = false;
        declare myStrExtra:s = "RedLang Test";
        declare myNullExtra:i? = null;

        declare scores:i[3] = [90, 85, 100];
        show("Array initial length: ");
        show(len(scores)); 
        show("\n");

        set scores[1] = 95; 
        set scores = [10, 20, 30, 40, 50]; 
        
        show("New array length: ");
        show(len(scores));
        show("\n");

        check ((myIntExtra > 40) and (not myBoolExtra)) {
            show("Condition passed.\n");
        } otherwise {
            show("Condition failed.\n");
        }

        declare counter:i = 0;
        loop (set counter = 1; counter <= 3; set counter = counter + 1) {
            show("Loop iteration: ");
            show(counter);
            show("\n");
        }

        declare limitVal:i = 2;
        repeat (limitVal > 0) {
            show("Repeat step: ");
            show(limitVal);
            show("\n");
            set limitVal = limitVal - 1;
        }

        declare player:Entity = Entity();
        player.Setup(1, "PlayerOne");
        player.Display();

        declare math:Calculator = Calculator();
        declare resMult:f = math.Multiply(2.5, 4.0);
        show("Sum Result: ");
        show(resMult);
        show("\n");

        show("Enter a number to double: ");
        declare inputStr:s;
        ask(inputStr); 
        
        declare convertedInt:i = convertToInt(inputStr); 
        show("Doubled value: ");
        show(convertedInt * 2);
        show("\n");

        show("Enter a decimal: ");
        ask(inputStr);
        declare valFVal:f = convertToFloat(inputStr);
        show("You entered float: ");
        show(valFVal);
        show("\n");

        gives 0;
    }
}
