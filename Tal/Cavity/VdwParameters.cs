// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Cavity detection / analysis module: VdwParameters.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Tal
{
 /// <summary>
 /// Central van der Waals radii table.
 /// SINGLE SOURCE OF TRUTH for all vdW radii in the system.
 ///
 /// ⭐ color[] — global atom colour table, index = atomic number.
 /// Modyfikowalna runtime przez AtomPropertiesWindow.
 /// DefaultColor[] — original copy, for Reset and comparisons.
 ///
 /// ⚠️ INITIALISATION ORDER (C#):
 /// 1. Afterla static inline — w order deklaracji w pliku
 /// 2. Konstruktor static
 ///
 /// Fortego: color[] MUSI be zadeklarowane PRZED DefaultColor[].
 /// DefaultColor[] inicjalizowane inline jako Clone() color[].
 /// </summary>
    public static class VdwParameters
    {
        #region Color Table (modyfikowalna runtime)

 /// <summary>
 /// ATOM COLOURS — index = atomic number.
 /// [0] = default szary (nie pierwiastek).
 ///
 /// ⚠️ THIS TABLE CAN BE MODIFIED AT RUNTIME (AtomPropertiesWindow).
 /// Kopia oryginalna finds w DefaultColor[].
 ///
 /// ⚠️ MUSI be zadeklarowana PRZED DefaultColor[] — inaczej Clone() below
 /// rzuci NullReferenceException.
 /// </summary>
        public static Vector3[] color =
        {
 // 0 - default
            new Vector3(0.5f, 0.5f, 0.5f),

 // Period 1
            new Vector3(1.00f, 1.00f, 1.00f),  // 1  H  — white
            new Vector3(0.85f, 1.00f, 1.00f),  // 2  He — jasnoniebieski

 // Period 2
            new Vector3(0.80f, 0.50f, 1.00f),  // 3  Li — fioletowy
            new Vector3(0.76f, 1.00f, 0.00f),  // 4  Be — limonkowy
            new Vector3(1.00f, 0.71f, 0.71f),  // 5  B  — pink
            new Vector3(0.60f, 0.60f, 0.60f),  // 6  C  — ciemnoszary (Jmol)
            new Vector3(0.50f, 0.30f, 1.00f),  // 7  N  — niebieski
            new Vector3(1.00f, 0.00f, 0.00f),  // 8  O  — czerwony
            new Vector3(0.20f, 0.80f, 0.80f),  // 9  F  — cyjan
            new Vector3(0.70f, 0.89f, 0.96f),  // 10 Ne — jasnoniebieski

 // Period 3
            new Vector3(0.67f, 0.36f, 0.95f),  // 11 Na — fioletowy
            new Vector3(0.54f, 1.00f, 0.00f),  // 12 Mg — zielony
            new Vector3(0.75f, 0.65f, 0.65f),  // 13 Al — grey-brown
            new Vector3(0.94f, 0.78f, 0.63f),  // 14 Si — piaskowy
            new Vector3(1.00f, 0.50f, 0.00f),  // 15 P  — pomarańczowy
            new Vector3(1.00f, 1.00f, 0.00f),  // 16 S  — żółty
            new Vector3(0.12f, 0.94f, 0.12f),  // 17 Cl — zielony (Jmol)
            new Vector3(0.50f, 0.82f, 0.89f),  // 18 Ar — jasnoniebieski

 // Period 4
            new Vector3(0.56f, 0.25f, 0.83f),  // 19 K  — fioletowy
            new Vector3(0.24f, 1.00f, 0.00f),  // 20 Ca — zielony
            new Vector3(0.90f, 0.90f, 0.90f),  // 21 Sc — jasnoszary
            new Vector3(0.75f, 0.76f, 0.78f),  // 22 Ti — srebrny
            new Vector3(0.65f, 0.65f, 0.67f),  // 23 V  — szary
            new Vector3(0.54f, 0.60f, 0.78f),  // 24 Cr — niebieskoszary
            new Vector3(0.61f, 0.48f, 0.78f),  // 25 Mn — fiołkowy
            new Vector3(0.88f, 0.40f, 0.20f),  // 26 Fe — rdzawy
            new Vector3(0.94f, 0.56f, 0.63f),  // 27 Co — pink metal
            new Vector3(0.31f, 0.82f, 0.31f),  // 28 Ni — zielony metal
            new Vector3(0.78f, 0.50f, 0.20f),  // 29 Cu — miedziany
            new Vector3(0.49f, 0.50f, 0.69f),  // 30 Zn — szaroniebieski
            new Vector3(0.76f, 0.56f, 0.56f),  // 31 Ga — szaropink
            new Vector3(0.40f, 0.56f, 0.56f),  // 32 Ge — szaroniebieski
            new Vector3(0.74f, 0.50f, 0.89f),  // 33 As — fioletowy
            new Vector3(1.00f, 0.63f, 0.00f),  // 34 Se — pomarańczowy
            new Vector3(0.65f, 0.16f, 0.16f),  // 35 Br — ciemny brąz
            new Vector3(0.36f, 0.72f, 0.82f),  // 36 Kr — jasnoniebieski

 // Period 5
            new Vector3(0.44f, 0.18f, 0.69f),  // 37 Rb — fioletowy
            new Vector3(0.00f, 1.00f, 0.00f),  // 38 Sr — zielony
            new Vector3(0.58f, 1.00f, 1.00f),  // 39 Y  — jasnocjan
            new Vector3(0.58f, 0.88f, 0.88f),  // 40 Zr — jasnoniebieski
            new Vector3(0.45f, 0.76f, 0.79f),  // 41 Nb — niebieskoszary
            new Vector3(0.33f, 0.71f, 0.71f),  // 42 Mo — szaroniebieski
            new Vector3(0.23f, 0.62f, 0.62f),  // 43 Tc — ciemnoniebieski
            new Vector3(0.14f, 0.56f, 0.56f),  // 44 Ru — ciemny cyjan
            new Vector3(0.04f, 0.49f, 0.55f),  // 45 Rh — ciemny niebieski
            new Vector3(0.00f, 0.41f, 0.52f),  // 46 Pd — ciemny błękit
            new Vector3(0.88f, 0.88f, 1.00f),  // 47 Ag — jasnosrebrny
            new Vector3(1.00f, 0.85f, 0.56f),  // 48 Cd — jasny żółty
            new Vector3(0.65f, 0.46f, 0.45f),  // 49 In — brązowy
            new Vector3(0.40f, 0.50f, 0.50f),  // 50 Sn — szary
            new Vector3(0.62f, 0.39f, 0.71f),  // 51 Sb — fioletowy
            new Vector3(0.83f, 0.48f, 0.00f),  // 52 Te — pomarańczowy
            new Vector3(0.58f, 0.00f, 0.58f),  // 53 I  — ciemny fiolet
            new Vector3(0.26f, 0.62f, 0.69f),  // 54 Xe — jasnoniebieski

 // Period 6
            new Vector3(0.34f, 0.09f, 0.56f),  // 55 Cs — fioletowy
            new Vector3(0.00f, 0.79f, 0.00f),  // 56 Ba — zielony
            new Vector3(0.44f, 0.83f, 1.00f),  // 57 La — jasnoniebieski
            new Vector3(1.00f, 1.00f, 0.78f),  // 58 Ce — jasny żółty
            new Vector3(0.85f, 1.00f, 0.78f),  // 59 Pr — jasnozielony
            new Vector3(0.78f, 1.00f, 0.78f),  // 60 Nd — jasnozielony
            new Vector3(0.64f, 1.00f, 0.78f),  // 61 Pm — jasnozielony
            new Vector3(0.56f, 1.00f, 0.78f),  // 62 Sm — jasnozielony
            new Vector3(0.38f, 1.00f, 0.78f),  // 63 Eu — jasnozielony
            new Vector3(0.27f, 1.00f, 0.78f),  // 64 Gd — jasnozielony
            new Vector3(0.19f, 1.00f, 0.78f),  // 65 Tb — jasnozielony
            new Vector3(0.12f, 1.00f, 0.78f),  // 66 Dy — jasnozielony
            new Vector3(0.00f, 1.00f, 0.61f),  // 67 Ho — zielony
            new Vector3(0.00f, 0.90f, 0.46f),  // 68 Er — zielony
            new Vector3(0.00f, 0.83f, 0.32f),  // 69 Tm — zielony
            new Vector3(0.00f, 0.75f, 0.22f),  // 70 Yb — zielony
            new Vector3(0.00f, 0.67f, 0.14f),  // 71 Lu — ciemnozielony
            new Vector3(0.30f, 0.76f, 1.00f),  // 72 Hf — jasnoniebieski
            new Vector3(0.30f, 0.65f, 1.00f),  // 73 Ta — jasnoniebieski
            new Vector3(0.13f, 0.58f, 0.84f),  // 74 W  — niebieski
            new Vector3(0.15f, 0.49f, 0.67f),  // 75 Re — ciemnoniebieski
            new Vector3(0.15f, 0.40f, 0.59f),  // 76 Os — niebieski
            new Vector3(0.09f, 0.33f, 0.53f),  // 77 Ir — ciemnoniebieski
            new Vector3(0.82f, 0.82f, 0.88f),  // 78 Pt — jasnosrebrny
            new Vector3(1.00f, 0.82f, 0.14f),  // 79 Au — złoty
            new Vector3(0.72f, 0.72f, 0.82f),  // 80 Hg — srebrzysty
            new Vector3(0.65f, 0.33f, 0.30f),  // 81 Tl — brązowy
            new Vector3(0.34f, 0.35f, 0.38f),  // 82 Pb — ciemnoszary
            new Vector3(0.62f, 0.31f, 0.71f),  // 83 Bi — fioletowy
            new Vector3(0.67f, 0.36f, 0.00f),  // 84 Po — brązowo-pomarańczowy
            new Vector3(0.46f, 0.31f, 0.27f),  // 85 At — brązowy
            new Vector3(0.26f, 0.51f, 0.59f),  // 86 Rn — niebieski

 // Period 7
            new Vector3(0.26f, 0.00f, 0.40f),  // 87 Fr — ciemny fiolet
            new Vector3(0.00f, 0.49f, 0.00f),  // 88 Ra — ciemnozielony
            new Vector3(0.44f, 0.67f, 0.98f),  // 89 Ac — jasnoniebieski
            new Vector3(0.00f, 0.73f, 1.00f),  // 90 Th — niebieski
            new Vector3(0.00f, 0.63f, 1.00f),  // 91 Pa — niebieski
            new Vector3(0.00f, 0.56f, 1.00f),  // 92 U  — niebieski
            new Vector3(0.00f, 0.50f, 1.00f),  // 93 Np — niebieski
            new Vector3(0.00f, 0.42f, 1.00f),  // 94 Pu — niebieski
            new Vector3(0.33f, 0.36f, 0.95f),  // 95 Am — niebieski
            new Vector3(0.47f, 0.36f, 0.89f),  // 96 Cm — niebieski
            new Vector3(0.54f, 0.31f, 0.89f),  // 97 Bk — niebieski
            new Vector3(0.63f, 0.21f, 0.83f),  // 98 Cf — niebieski
            new Vector3(0.70f, 0.12f, 0.83f),  // 99 Es — niebieski
            new Vector3(0.70f, 0.12f, 0.73f),  // 100 Fm — niebieski
            new Vector3(0.70f, 0.05f, 0.65f),  // 101 Md — niebieski
            new Vector3(0.74f, 0.05f, 0.53f),  // 102 No — niebieski
            new Vector3(0.78f, 0.00f, 0.40f),  // 103 Lr — niebieski
            new Vector3(0.80f, 0.00f, 0.35f),  // 104 Rf
            new Vector3(0.82f, 0.00f, 0.31f),  // 105 Db
            new Vector3(0.85f, 0.00f, 0.27f),  // 106 Sg
            new Vector3(0.88f, 0.00f, 0.22f),  // 107 Bh
            new Vector3(0.90f, 0.00f, 0.18f),  // 108 Hs
            new Vector3(0.92f, 0.00f, 0.15f),  // 109 Mt
            new Vector3(0.93f, 0.00f, 0.12f),  // 110 Ds
            new Vector3(0.94f, 0.00f, 0.10f),  // 111 Rg
            new Vector3(0.95f, 0.00f, 0.08f),  // 112 Cn
            new Vector3(0.96f, 0.00f, 0.06f),  // 113 Nh
            new Vector3(0.97f, 0.00f, 0.05f),  // 114 Fl
            new Vector3(0.98f, 0.00f, 0.04f),  // 115 Mc
            new Vector3(0.99f, 0.00f, 0.03f),  // 116 Lv
            new Vector3(1.00f, 0.00f, 0.02f),  // 117 Ts
            new Vector3(1.00f, 0.00f, 0.00f),  // 118 Og
        };

 /// <summary>
 /// ORIGINAL colour table copy — NEVER modified.
 ///
 /// ⚠️ MUST be declared AFTER color[] (so that Clone() works).
 /// ⚠️ Initialised inline — so there is no risk that ResetAllColors()
 /// will be called before the field exists.
 /// </summary>
        public static readonly Vector3[] DefaultColor = (Vector3[])color.Clone();

        #endregion

        #region Element Symbols & Names (do UI)

 /// <summary>
 /// Element symbols — index = atomic number. [0] = "" (default).
 /// </summary>
        public static readonly string[] ElementSymbols =
        {
 // 0
            "",
 // 1-10
            "H",  "He", "Li", "Be", "B",  "C",  "N",  "O",  "F",  "Ne",
 // 11-20
            "Na", "Mg", "Al", "Si", "P",  "S",  "Cl", "Ar", "K",  "Ca",
 // 21-30
            "Sc", "Ti", "V",  "Cr", "Mn", "Fe", "Co", "Ni", "Cu", "Zn",
 // 31-40
            "Ga", "Ge", "As", "Se", "Br", "Kr", "Rb", "Sr", "Y",  "Zr",
 // 41-50
            "Nb", "Mo", "Tc", "Ru", "Rh", "Pd", "Ag", "Cd", "In", "Sn",
 // 51-60
            "Sb", "Te", "I",  "Xe", "Cs", "Ba", "La", "Ce", "Pr", "Nd",
 // 61-70
            "Pm", "Sm", "Eu", "Gd", "Tb", "Dy", "Ho", "Er", "Tm", "Yb",
 // 71-80
            "Lu", "Hf", "Ta", "W",  "Re", "Os", "Ir", "Pt", "Au", "Hg",
 // 81-90
            "Tl", "Pb", "Bi", "Po", "At", "Rn", "Fr", "Ra", "Ac", "Th",
 // 91-100
            "Pa", "U",  "Np", "Pu", "Am", "Cm", "Bk", "Cf", "Es", "Fm",
 // 101-110
            "Md", "No", "Lr", "Rf", "Db", "Sg", "Bh", "Hs", "Mt", "Ds",
 // 111-118
            "Rg", "Cn", "Nh", "Fl", "Mc", "Lv", "Ts", "Og"
        };

 /// <summary>
 /// Full element names (IUPAC, English) — index = atomic number. [0] = "".
 /// </summary>
        public static readonly string[] ElementNames =
        {
 // 0
            "",
 // 1-10
            "Hydrogen",     "Helium",       "Lithium",      "Beryllium",    "Boron",
            "Carbon",       "Nitrogen",     "Oxygen",       "Fluorine",     "Neon",
 // 11-20
            "Sodium",       "Magnesium",    "Aluminium",    "Silicon",      "Phosphorus",
            "Sulfur",       "Chlorine",     "Argon",        "Potassium",    "Calcium",
 // 21-30
            "Scandium",     "Titanium",     "Vanadium",     "Chromium",     "Manganese",
            "Iron",         "Cobalt",       "Nickel",       "Copper",       "Zinc",
 // 31-40
            "Gallium",      "Germanium",    "Arsenic",      "Selenium",     "Bromine",
            "Krypton",      "Rubidium",     "Strontium",    "Yttrium",      "Zirconium",
 // 41-50
            "Niobium",      "Molybdenum",   "Technetium",   "Ruthenium",    "Rhodium",
            "Palladium",    "Silver",       "Cadmium",      "Indium",       "Tin",
 // 51-60
            "Antimony",     "Tellurium",    "Iodine",       "Xenon",        "Caesium",
            "Barium",       "Lanthanum",    "Cerium",       "Praseodymium", "Neodymium",
 // 61-70
            "Promethium",   "Samarium",     "Europium",     "Gadolinium",   "Terbium",
            "Dysprosium",   "Holmium",      "Erbium",       "Thulium",      "Ytterbium",
 // 71-80
            "Lutetium",     "Hafnium",      "Tantalum",     "Tungsten",     "Rhenium",
            "Osmium",       "Iridium",      "Platinum",     "Gold",         "Mercury",
 // 81-90
            "Thallium",     "Lead",         "Bismuth",      "Polonium",     "Astatine",
            "Radon",        "Francium",     "Radium",       "Actinium",     "Thorium",
 // 91-100
            "Protactinium", "Uranium",      "Neptunium",    "Plutonium",    "Americium",
            "Curium",       "Berkelium",    "Californium",  "Einsteinium",  "Fermium",
 // 101-110
            "Mendelevium",  "Nobelium",     "Lawrencium",   "Rutherfordium","Dubnium",
            "Seaborgium",   "Bohrium",      "Hassium",      "Meitnerium",   "Darmstadtium",
 // 111-118
            "Roentgenium",  "Copernicium",  "Nihonium",     "Flerovium",    "Moscovium",
            "Livermorium",  "Tennessine",   "Oganesson"
        };

        #endregion

        #region Helper Methods (kolor)

 /// <summary>
 /// Przywraca oryginalny colour pierwiastka.
 /// </summary>
        public static void ResetColor(int atomicNumber)
        {
            if (atomicNumber < 0 || atomicNumber >= color.Length) return;
            if (atomicNumber >= DefaultColor.Length) return;
            color[atomicNumber] = DefaultColor[atomicNumber];
        }

 /// <summary>
 /// Restores original colours of all elements.
 /// </summary>
        public static void ResetAllColors()
        {
            int n = Math.Min(color.Length, DefaultColor.Length);
            for (int i = 0; i < n; i++)
                color[i] = DefaultColor[i];
        }

 /// <summary>
 /// Checks whether the element colour differs from the original.
 /// </summary>
        public static bool IsColorModified(int atomicNumber)
        {
            if (atomicNumber < 0 || atomicNumber >= color.Length) return false;
            if (atomicNumber >= DefaultColor.Length) return false;

            var c = color[atomicNumber];
            var d = DefaultColor[atomicNumber];

            return Math.Abs(c.X - d.X) > 1e-4f ||
                   Math.Abs(c.Y - d.Y) > 1e-4f ||
                   Math.Abs(c.Z - d.Z) > 1e-4f;
        }

        #endregion

        #region Static Tables (private)

 // Main table - atom name -> radius
        private static readonly Dictionary<string, float> _vdwRadiiByName;

 // Table for atomic numbers
        private static readonly float[] _vdwRadiiByAtomicNumber;

 // Ionic radii table
        private static readonly float[] _ionicRadii;

 // Name to atomic number mapping
        private static readonly Dictionary<string, int> _nameToNumber;

        #endregion

        #region Static Constructor

 /// <summary>
 /// Fills dictionaries and tables of vdW / ionic radii.
 ///
 /// ⚠️ Does NOT touch color[] or DefaultColor[] — those are initialised inline
 /// (see top of file). Here only radius dictionaries and tables.
 /// </summary>
        static VdwParameters()
        {
 // Initialize name->radius dictionary
            _vdwRadiiByName = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
            {
 // Period 1
                { "H", 1.20f },  { "D", 1.20f },  { "T", 1.20f },
                { "He", 1.40f },

 // Period 2
                { "Li", 1.82f }, { "Be", 1.86f }, { "B", 1.74f },
                { "C", 1.70f },  { "C1", 1.70f }, { "C2", 1.70f }, { "C3", 1.70f },
                { "N", 1.55f },  { "N1", 1.55f }, { "N2", 1.55f }, { "N3", 1.55f },
                { "O", 1.52f },  { "O1", 1.52f }, { "O2", 1.52f }, { "O3", 1.52f },
                { "F", 1.47f },  { "F1", 1.47f },
                { "Ne", 1.54f },

 // Period 3
                { "Na", 2.27f }, { "Mg", 1.73f }, { "Al", 1.90f },
                { "Si", 2.10f }, { "Si1", 2.10f },
                { "P", 1.80f },  { "P1", 1.80f },
                { "S", 1.80f },  { "S1", 1.80f }, { "S2", 1.80f },
                { "Cl", 1.89f }, { "Cl1", 1.89f },
                { "Ar", 1.88f },

 // Period 4
                { "K", 2.75f },  { "Ca", 2.61f }, { "Sc", 2.28f },
                { "Ti", 2.14f }, { "V", 2.03f },
                { "Cr", 1.97f }, { "Cr1", 1.97f }, { "Cr2", 1.97f }, { "Cr3", 1.97f },
                { "Mn", 1.96f }, { "Mn1", 1.96f }, { "Mn2", 1.96f }, { "Mn3", 1.96f },
                { "Fe", 1.96f }, { "Fe1", 1.96f }, { "Fe2", 1.96f }, { "Fe3", 1.96f },
                { "Co", 1.95f }, { "Co1", 1.95f }, { "Co2", 1.95f }, { "Co3", 1.95f },
                { "Ni", 1.63f }, { "Ni1", 1.63f }, { "Ni2", 1.63f }, { "Ni3", 1.63f },
                { "Cu", 1.40f }, { "Cu1", 1.40f }, { "Cu2", 1.40f },
                { "Zn", 1.39f }, { "Zn1", 1.39f }, { "Zn2", 1.39f },
                { "Ga", 1.87f }, { "Ge", 2.13f }, { "As", 1.85f },
                { "Se", 1.90f }, { "Br", 1.85f }, { "Br1", 1.85f },
                { "Kr", 2.02f },

 // Period 5
                { "Rb", 3.12f }, { "Sr", 2.45f }, { "Y", 2.45f },
                { "Zr", 2.25f }, { "Nb", 2.13f }, { "Mo", 2.06f },
                { "Tc", 2.04f }, { "Ru", 2.02f }, { "Rh", 2.02f },
                { "Pd", 1.63f }, { "Ag", 1.72f }, { "Ag1", 1.72f }, { "Ag2", 1.72f },
                { "Cd", 1.58f }, { "Cd1", 1.58f }, { "Cd2", 1.58f },
                { "In", 1.93f }, { "Sn", 2.20f }, { "Sn2", 2.20f }, { "Sn4", 2.20f },
                { "Sb", 2.33f }, { "Te", 2.06f },
                { "I", 1.98f },  { "I1", 1.98f },
                { "Xe", 2.16f },

 // Period 6
                { "Cs", 3.31f }, { "Ba", 2.85f }, { "La", 2.51f },
                { "Ce", 3.31f }, { "Pr", 2.20f }, { "Nd", 2.45f },
                { "Pm", 2.47f }, { "Sm", 2.20f }, { "Eu", 2.20f },
                { "Gd", 2.20f }, { "Tb", 2.20f }, { "Dy", 2.45f },
                { "Ho", 2.20f }, { "Er", 2.20f }, { "Tm", 2.20f },
                { "Yb", 2.20f }, { "Lu", 2.20f },
                { "Hf", 2.20f }, { "Ta", 2.20f }, { "W", 2.20f },
                { "Re", 2.20f }, { "Os", 2.20f }, { "Ir", 2.03f },
                { "Pt", 1.75f }, { "Pt1", 1.75f }, { "Pt2", 1.75f },
                { "Au", 1.69f }, { "Au1", 1.69f }, { "Au3", 1.69f },
                { "Hg", 1.55f }, { "Hg1", 1.55f }, { "Hg2", 1.55f },
                { "Tl", 1.96f }, { "Pb", 2.02f }, { "Pb2", 2.02f }, { "Pb4", 2.02f },
                { "Bi", 2.42f }, { "Po", 2.20f }, { "At", 2.20f },
                { "Rn", 2.20f },

 // Period 7
                { "Fr", 2.20f }, { "Ra", 2.20f }, { "Ac", 2.20f },
                { "Th", 2.20f }, { "Pa", 2.20f }, { "U", 1.86f },
                { "Np", 1.80f }, { "Pu", 1.80f }, { "Am", 1.80f },
                { "Cm", 1.80f }, { "Bk", 1.80f }, { "Cf", 1.80f },
                { "Es", 1.80f }, { "Fm", 1.80f }, { "Md", 1.80f },
                { "No", 1.80f }, { "Lr", 1.80f },
            };

 // Name to atomic number mapping
            _nameToNumber = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "H", 1 }, { "D", 1 }, { "T", 1 }, { "He", 2 },
                { "Li", 3 }, { "Be", 4 }, { "B", 5 }, { "C", 6 },
                { "C1", 6 }, { "C2", 6 }, { "C3", 6 },
                { "N", 7 }, { "N1", 7 }, { "N2", 7 }, { "N3", 7 },
                { "O", 8 }, { "O1", 8 }, { "O2", 8 }, { "O3", 8 },
                { "F", 9 }, { "F1", 9 }, { "Ne", 10 },
                { "Na", 11 }, { "Mg", 12 }, { "Al", 13 },
                { "Si", 14 }, { "Si1", 14 }, { "P", 15 }, { "P1", 15 },
                { "S", 16 }, { "S1", 16 }, { "S2", 16 },
                { "Cl", 17 }, { "Cl1", 17 }, { "Ar", 18 },
                { "K", 19 }, { "Ca", 20 }, { "Sc", 21 },
                { "Ti", 22 }, { "V", 23 },
                { "Cr", 24 }, { "Cr1", 24 }, { "Cr2", 24 }, { "Cr3", 24 },
                { "Mn", 25 }, { "Mn1", 25 }, { "Mn2", 25 }, { "Mn3", 25 },
                { "Fe", 26 }, { "Fe1", 26 }, { "Fe2", 26 }, { "Fe3", 26 },
                { "Co", 27 }, { "Co1", 27 }, { "Co2", 27 }, { "Co3", 27 },
                { "Ni", 28 }, { "Ni1", 28 }, { "Ni2", 28 }, { "Ni3", 28 },
                { "Cu", 29 }, { "Cu1", 29 }, { "Cu2", 29 },
                { "Zn", 30 }, { "Zn1", 30 }, { "Zn2", 30 },
                { "Ga", 31 }, { "Ge", 32 }, { "As", 33 },
                { "Se", 34 }, { "Br", 35 }, { "Br1", 35 },
                { "Kr", 36 }, { "Rb", 37 }, { "Sr", 38 },
                { "Y", 39 }, { "Zr", 40 }, { "Nb", 41 },
                { "Mo", 42 }, { "Tc", 43 }, { "Ru", 44 },
                { "Rh", 45 }, { "Pd", 46 },
                { "Ag", 47 }, { "Ag1", 47 }, { "Ag2", 47 },
                { "Cd", 48 }, { "Cd1", 48 }, { "Cd2", 48 },
                { "In", 49 }, { "Sn", 50 }, { "Sn2", 50 }, { "Sn4", 50 },
                { "Sb", 51 }, { "Te", 52 }, { "I", 53 }, { "I1", 53 },
                { "Xe", 54 }, { "Cs", 55 }, { "Ba", 56 },
                { "La", 57 }, { "Ce", 58 }, { "Pr", 59 },
                { "Nd", 60 }, { "Pm", 61 }, { "Sm", 62 },
                { "Eu", 63 }, { "Gd", 64 }, { "Tb", 65 },
                { "Dy", 66 }, { "Ho", 67 }, { "Er", 68 },
                { "Tm", 69 }, { "Yb", 70 }, { "Lu", 71 },
                { "Hf", 72 }, { "Ta", 73 }, { "W", 74 },
                { "Re", 75 }, { "Os", 76 }, { "Ir", 77 },
                { "Pt", 78 }, { "Pt1", 78 }, { "Pt2", 78 },
                { "Au", 79 }, { "Au1", 79 }, { "Au3", 79 },
                { "Hg", 80 }, { "Hg1", 80 }, { "Hg2", 80 },
                { "Tl", 81 }, { "Pb", 82 }, { "Pb2", 82 }, { "Pb4", 82 },
                { "Bi", 83 }, { "Po", 84 }, { "At", 85 },
                { "Rn", 86 }, { "Fr", 87 }, { "Ra", 88 },
                { "Ac", 89 }, { "Th", 90 }, { "Pa", 91 },
                { "U", 92 }, { "Np", 93 }, { "Pu", 94 },
                { "Am", 95 }, { "Cm", 96 }, { "Bk", 97 },
                { "Cf", 98 }, { "Es", 99 }, { "Fm", 100 },
                { "Md", 101 }, { "No", 102 }, { "Lr", 103 },
            };

 // Initialize atomic number table
            _vdwRadiiByAtomicNumber = new float[120];
            for (int i = 0; i < _vdwRadiiByAtomicNumber.Length; i++)
                _vdwRadiiByAtomicNumber[i] = 1.70f; // Default carbon

            foreach (var kvp in _vdwRadiiByName)
            {
                if (_nameToNumber.TryGetValue(kvp.Key, out int number))
                {
                    if (number >= 0 && number < _vdwRadiiByAtomicNumber.Length)
                        _vdwRadiiByAtomicNumber[number] = kvp.Value;
                }
            }

 // Initialize ionic radii table
            _ionicRadii = new float[120];
            for (int i = 0; i < _ionicRadii.Length; i++)
                _ionicRadii[i] = 1.0f;

            _ionicRadii[1] = 0.25f;   // H
            _ionicRadii[2] = 0.31f;   // He
            _ionicRadii[3] = 1.45f;   // Li
            _ionicRadii[4] = 1.05f;   // Be
            _ionicRadii[5] = 0.85f;   // B
            _ionicRadii[6] = 0.70f;   // C
            _ionicRadii[7] = 0.65f;   // N
            _ionicRadii[8] = 0.60f;   // O
            _ionicRadii[9] = 0.50f;   // F
            _ionicRadii[10] = 0.38f;  // Ne
            _ionicRadii[11] = 1.80f;  // Na
            _ionicRadii[12] = 1.50f;  // Mg
            _ionicRadii[13] = 1.25f;  // Al
            _ionicRadii[14] = 1.10f;  // Si
            _ionicRadii[15] = 1.00f;  // P
            _ionicRadii[16] = 1.00f;  // S
            _ionicRadii[17] = 1.00f;  // Cl
            _ionicRadii[18] = 0.71f;  // Ar
            _ionicRadii[19] = 2.20f;  // K
            _ionicRadii[20] = 1.80f;  // Ca
            _ionicRadii[21] = 1.60f;  // Sc
            _ionicRadii[22] = 1.40f;  // Ti
            _ionicRadii[23] = 1.35f;  // V
            _ionicRadii[24] = 1.40f;  // Cr
            _ionicRadii[25] = 1.40f;  // Mn
            _ionicRadii[26] = 1.40f;  // Fe
            _ionicRadii[27] = 1.35f;  // Co
            _ionicRadii[28] = 1.35f;  // Ni
            _ionicRadii[29] = 1.35f;  // Cu
            _ionicRadii[30] = 1.35f;  // Zn
            _ionicRadii[31] = 1.30f;  // Ga
            _ionicRadii[32] = 1.25f;  // Ge
            _ionicRadii[33] = 1.15f;  // As
            _ionicRadii[34] = 1.15f;  // Se
            _ionicRadii[35] = 1.15f;  // Br
            _ionicRadii[36] = 0.88f;  // Kr
            _ionicRadii[37] = 2.35f;  // Rb
            _ionicRadii[38] = 2.00f;  // Sr
            _ionicRadii[39] = 1.85f;  // Y
            _ionicRadii[40] = 1.55f;  // Zr
            _ionicRadii[41] = 1.45f;  // Nb
            _ionicRadii[42] = 1.45f;  // Mo
            _ionicRadii[43] = 1.35f;  // Tc
            _ionicRadii[44] = 1.30f;  // Ru
            _ionicRadii[45] = 1.35f;  // Rh
            _ionicRadii[46] = 1.40f;  // Pd
            _ionicRadii[47] = 1.60f;  // Ag
            _ionicRadii[48] = 1.55f;  // Cd
            _ionicRadii[49] = 1.55f;  // In
            _ionicRadii[50] = 1.45f;  // Sn
            _ionicRadii[51] = 1.45f;  // Sb
            _ionicRadii[52] = 1.40f;  // Te
            _ionicRadii[53] = 1.40f;  // I
            _ionicRadii[54] = 1.08f;  // Xe
            _ionicRadii[55] = 2.60f;  // Cs
            _ionicRadii[56] = 2.15f;  // Ba
            _ionicRadii[57] = 1.95f;  // La
            _ionicRadii[58] = 1.85f;  // Ce
            _ionicRadii[59] = 1.85f;  // Pr
            _ionicRadii[60] = 1.85f;  // Nd
            _ionicRadii[61] = 1.85f;  // Pm
            _ionicRadii[62] = 1.85f;  // Sm
            _ionicRadii[63] = 1.85f;  // Eu
            _ionicRadii[64] = 1.80f;  // Gd
            _ionicRadii[65] = 1.75f;  // Tb
            _ionicRadii[66] = 1.75f;  // Dy
            _ionicRadii[67] = 1.75f;  // Ho
            _ionicRadii[68] = 1.75f;  // Er
            _ionicRadii[69] = 1.75f;  // Tm
            _ionicRadii[70] = 1.75f;  // Yb
            _ionicRadii[71] = 1.75f;  // Lu
            _ionicRadii[72] = 1.55f;  // Hf
            _ionicRadii[73] = 1.45f;  // Ta
            _ionicRadii[74] = 1.35f;  // W
            _ionicRadii[75] = 1.35f;  // Re
            _ionicRadii[76] = 1.30f;  // Os
            _ionicRadii[77] = 1.35f;  // Ir
            _ionicRadii[78] = 1.35f;  // Pt
            _ionicRadii[79] = 1.35f;  // Au
            _ionicRadii[80] = 1.50f;  // Hg
            _ionicRadii[81] = 1.90f;  // Tl
            _ionicRadii[82] = 1.80f;  // Pb
            _ionicRadii[83] = 1.60f;  // Bi
            _ionicRadii[84] = 1.90f;  // Po
            _ionicRadii[85] = 1.27f;  // At
            _ionicRadii[86] = 1.20f;  // Rn
            _ionicRadii[87] = 2.60f;  // Fr
            _ionicRadii[88] = 2.15f;  // Ra
            _ionicRadii[89] = 1.95f;  // Ac
            _ionicRadii[90] = 1.80f;  // Th
            _ionicRadii[91] = 1.80f;  // Pa
            _ionicRadii[92] = 1.75f;  // U
            _ionicRadii[93] = 1.75f;  // Np
            _ionicRadii[94] = 1.75f;  // Pu
            _ionicRadii[95] = 1.75f;  // Am
        }

        #endregion

        #region Public Access Methods

 /// <summary>
 /// Gets the van der Waals radius for an atom name (e.g., "C", "C1", "Fe").
 /// </summary>
        public static float GetRadius(string atomName)
        {
            if (string.IsNullOrEmpty(atomName))
                return 1.70f;

 // Direct match
            if (_vdwRadiiByName.TryGetValue(atomName, out float radius))
                return radius;

 // Try without trailing digits (e.g., "C1" -> "C")
            if (atomName.Length > 1)
            {
                string baseName = TrimEndDigits(atomName);
                if (baseName != atomName && _vdwRadiiByName.TryGetValue(baseName, out radius))
                    return radius;
            }

 // Try without +/- signs
            if (atomName.EndsWith("+") || atomName.EndsWith("-"))
            {
                string baseName = atomName.TrimEnd('+', '-');
                if (_vdwRadiiByName.TryGetValue(baseName, out radius))
                    return radius;
            }

 // If atom has a number, use it
            if (_nameToNumber.TryGetValue(atomName, out int number))
                return GetRadius(number);

 // If atom has a number with trailing digits
            if (atomName.Length > 1)
            {
                string baseName = TrimEndDigits(atomName);
                if (_nameToNumber.TryGetValue(baseName, out number))
                    return GetRadius(number);
            }

            return 1.70f;
        }

 /// <summary>
 /// Gets the van der Waals radius for an atomic number.
 /// </summary>
        public static float GetRadius(int atomicNumber)
        {
            if (atomicNumber >= 0 && atomicNumber < _vdwRadiiByAtomicNumber.Length)
                return _vdwRadiiByAtomicNumber[atomicNumber];
            return 1.70f;
        }

 /// <summary>
 /// Gets the ionic radius for an atomic number.
 /// </summary>
        public static float GetIonicRadius(int atomicNumber)
        {
            if (atomicNumber >= 0 && atomicNumber < _ionicRadii.Length)
                return _ionicRadii[atomicNumber];
            return 1.0f;
        }

 /// <summary>
 /// Gets the ionic radius for an atom name.
 /// </summary>
        public static float GetIonicRadius(string atomName)
        {
            if (string.IsNullOrEmpty(atomName))
                return 1.0f;

            if (_nameToNumber.TryGetValue(atomName, out int number))
                return GetIonicRadius(number);

            if (atomName.Length > 1)
            {
                string baseName = TrimEndDigits(atomName);
                if (_nameToNumber.TryGetValue(baseName, out number))
                    return GetIonicRadius(number);
            }

            return 1.0f;
        }

 /// <summary>
 /// Gets the atomic number for an atom name.
 /// </summary>
        public static int GetAtomicNumber(string atomName)
        {
            if (string.IsNullOrEmpty(atomName))
                return 6;

            if (_nameToNumber.TryGetValue(atomName, out int number))
                return number;

            if (atomName.Length > 1)
            {
                string baseName = TrimEndDigits(atomName);
                if (_nameToNumber.TryGetValue(baseName, out number))
                    return number;
            }

            return 6;
        }

 /// <summary>
 /// Checks whether an atom name exists in the table.
 /// </summary>
        public static bool ContainsAtom(string atomName)
        {
            if (string.IsNullOrEmpty(atomName)) return false;
            return _vdwRadiiByName.ContainsKey(atomName) || _nameToNumber.ContainsKey(atomName);
        }

 /// <summary>
 /// Gets all available atom names.
 /// </summary>
        public static IEnumerable<string> GetAllAtomNames()
        {
            return _vdwRadiiByName.Keys;
        }

        #endregion

        #region Helper Methods

        private static string TrimEndDigits(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            int i = input.Length - 1;
            while (i >= 0 && char.IsDigit(input[i]))
                i--;
            return input.Substring(0, i + 1);
        }

        #endregion
    }
}