module Color

///Functions and types for color coding, decoding and conversions of hex colors
module Hex =
    
    open System

    ///Converts integer to hex based character (e.g. 1 -> '1', 11 -> 'B')
    [<CompiledName("ToHexDigit")>]
    let toHexDigit n =
        if n < 10 then char (n + 0x30) else char (n + 0x37)
    
    ///Converts a hex based character to an integer (e.g. '1' -> 1, 'B' -> 11)
    [<CompiledName("FromHexDigit")>]
    let fromHexDigit c =
        if c >= '0' && c <= '9' then int c - int '0'
        elif c >= 'A' && c <= 'F' then (int c - int 'A') + 10
        elif c >= 'a' && c <= 'f' then (int c - int 'a') + 10
        else raise <| new ArgumentException()
    
    ///Encodes a color byte array to a hex string with the given prefix 
    [<CompiledName("Encode")>]
    let encode (prefix:string) (color:byte array)  =
        let hex = Array.zeroCreate (color.Length * 2)
        let mutable n = 0
        for i = 0 to color.Length - 1 do
            hex.[n] <- toHexDigit ((int color.[i] &&& 0xF0) >>> 4)
            n <- n + 1
            hex.[n] <- toHexDigit (int color.[i] &&& 0xF)
            n <- n + 1
        String.Concat(prefix, new String(hex))
//        if prefix then String.Concat("0x", new String(hex)) 
//        else new String(hex)
            
    [<CompiledName("Decode")>]
    ///Decodes a color byte array from a hex string
    let decode (s:string) =
        match s with
        | null -> nullArg "s"
        | _ when s.Length = 0 -> Array.empty
        | _ ->
            let mutable len = s.Length
            let mutable i = 0
            if len >= 2 && s.[0] = '0' && (s.[1] = 'x' || s.[1] = 'X') then do
                len <- len - 2
                i <- i + 2
            if len % 2 <> 0 then failwithf "Invalid hex format: %s" s
            else
                let buf = Array.zeroCreate (len / 2)
                let mutable n = 0
                while i < s.Length do
                    buf.[n] <- byte (((fromHexDigit s.[i]) <<< 4) ||| (fromHexDigit s.[i + 1]))
                    i <- i + 2
                    n <- n + 1
                buf

//http://www.niwa.nu/2013/05/math-behind-colorspace-conversions-rgb-hsl/
///Module to create and manipulate ARGB colors
module Colors =
    
    /// Color component ARGB
    type ColorComponent =
        | A of byte
        | R of byte
        | G of byte
        | B of byte 
    
    /// returns the value hold by a color component
    let getValueFromCC cc =
        match cc with
        | A v -> v
        | R v -> v
        | G v -> v
        | B v -> v

    ///Represents an ARGB (alpha, red, green, blue) color
    type Color = {
        /// The alpha component value of this Color structure.
        A : byte
        /// The red component value of this Color structure.
        R : byte
        /// The green component value of this Color structure.
        G : byte
        /// The blue component value of this Color structure.
        B : byte
        }

    ///returns the maximum value of the R, G, and B components of a color
    let maxRGB c =
        let r,g,b = R c.R,G c.G,B c.B
        max r g |> max b

    ///returns the minimum value of the R, G, and B components of a color
    let minRGB c =
        let r,g,b = R c.R,G c.G,B c.B
        min r g |> min b
        


    /// Creates a Color structure from the four ARGB components (alpha, red, green, and blue) values.
    let fromArgb a r g b =
        let f v =
            if v < 0 || v > 255 then 
                failwithf "Value for component needs to be between 0 and 255."
            else
                byte v
        {A= f a; R = f r; G = f g; B = f b}

    /// Creates a Color structure from the specified color component values (red, green, and blue).
    /// The alpha value is implicitly 255 (fully opaque). 
    let fromRgb r g b =
        fromArgb 255 r g b

//    /// Gets the hue-saturation-brightness (HSB) brightness value for this Color structure.
//    let getBrightness = ()

    /// Gets the hue component value of the hue-saturation-brightness (HSB) format, in degrees, for this Color structure.
    let getHue c =
        let min = minRGB c |> getValueFromCC
        match maxRGB c with
        | R r -> float (c.G - c.B) / float (r - min)
        | G g -> 2.0 + float (c.B - c. R) / float (g - min)
        | B b -> 4.0 + float (c.R - c.G) / float (b - min)
        | _   -> failwithf "" // can't be


    /// Gets the saturation component value of the hue-saturation-brightness (HSB) format for this Color structure.
    let getSaturation col =
        let minimum = minRGB col
        let maximum = maxRGB col
        float (getValueFromCC minimum + getValueFromCC maximum) / 2.
        |> round
           
    /// Gets the 32-bit ARGB value of this Color structure.
    let toArgb c =
        (int c.A, int c.R, int c.G, int c.B)
    
    /// Gets the hex representataion (FFFFFF) of a color (with valid prefix "0xFFFFFF")
    let toHex prefix c =
        let prefix' = if prefix then "0x" else ""
        Hex.encode prefix' [|c.R;c.G;c.B|]                

    /// Gets color from hex representataion (FFFFFF) or (0xFFFFFF)
    let fromHex (s:string) =
        match (Hex.decode s) with
        | [|r;g;b|]  -> fromRgb (int r) (int g) (int b)
        | _          -> failwithf "Invalid hex color format"

    /// Gets the web color representataion (#FFFFFF)
    let toWebColor c =        
        Hex.encode "#" [|c.R;c.G;c.B|]                

    /// Gets color from web color (#FFFFFF)
    let fromWebColor (s:string) =
        let s' = s.TrimStart([|'#'|])
        match (Hex.decode s') with
        | [|r;g;b|]  -> fromRgb (int r) (int g) (int b)
        | _          -> failwithf "Invalid hex color format"


    /// Converts this Color structure to a human-readable string.
    let toString c =
        let a,r,g,b = toArgb c
        sprintf "{Alpha: %i Red: %i Green: %i Blue: %i}" a r g b

    let toForegroundTextColor (c:Color) = 
        if ((float c.R)*0.299 + (float c.G)*0.587 + (float c.B)*0.114) > 186. 
            then "000000" |> fromHex
            else "ffffff" |> fromHex

    open System.IO
    open System 

module Console =
    open Colors
    open Pastel

    let setBackgroundColor (c:Color) (displayString: string) =
        displayString.PastelBg((c |> toHex false))

    let setForegroundColor (c:Color) (displayString: string) =
        displayString.Pastel((c |> toHex false))
