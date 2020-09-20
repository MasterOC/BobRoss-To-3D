using UnityEngine;

public class ColorPicker{

    public static Color32 getColor(Colors c){
        switch(c){
            case Colors.WIESE:
                //#74DF00 - WIESE
                return new Color32(0x74, 0xDF, 0x00, 0xFF);
            case Colors.WALD:
                //#0B610B - WALD
                return new Color32(0x0B, 0x61, 0x0B, 0xFF);
            case Colors.BERG:
                //#848484 - BERG
                return new Color32(0x84, 0x84, 0x84, 0xFF);
            case Colors.WASSER:
                //#0000FF - WASSER
                return new Color32(0x00, 0x00, 0xFF, 0xFF);
            case Colors.SCHNEE:
                //#FFFFFF - WASSER
                return new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        }

        //return white
        return new Color32(0xFF, 0xFF, 0xFF, 0xFF);
    }

    public static Colors getColorFromHex(string hex){
        switch(hex){
            case "74DF00":
                //#74DF00 - WIESE
                return Colors.WIESE;
            case "0B610B":
                //#0B610B - WALD
                return Colors.WALD;
            case "848484":
                //#848484 - BERG
                return Colors.BERG;
            case "0000FF":
                //#0000FF - WASSER
                return Colors.WASSER;
            case "FFFFFF":
                //#FFFFFF - SCHNEE
                return Colors.SCHNEE;
        }

        //return white
        return Colors.WIESE;
    }

}