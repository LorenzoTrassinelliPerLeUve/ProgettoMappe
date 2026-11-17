# Azoto sotto 0,1 per mille e C/N: la regola (task #323, dba, 17/11/2026)

**Decisione del proprietario, 17/11/2026:** per Tikves 2023 il C/N si segna **come non valido**.

Regola per il report:
1. L'azoto totale in per mille è `Suolo_N_totale_permille` se c'è, altrimenti `Suolo_N_totale_perc × 10`. Nel 4g quasi sempre
   c'è solo la percentuale (3.332 rilievi 144 contro 13).
2. Se l'azoto è **sotto 0,1 per mille**, il dato è errato: non si usa e il C/N **non si calcola** da C e N.
3. In quel caso il C/N è quello del laboratorio (`Suolo_C_N`), se è plausibile. Se anche quello è palesemente errato, come per
   **Tikves 2023** (151-154), il C/N si mostra **come «non valido»**.

Rilievi coinvolti (100 su 7.855), dettaglio in `azoto-sotto-01-permille.xlsx`:
- Casa E. Mirafiore (425): 76 nel 2024, 22 dei quali a zero, con C/N del laboratorio fra 1 e 10, e 8 nel 2023 (C/N fra 3,5 e 8,7);
- TIKVES - GEVGELIJA (1864): 11 nel 2023, **C/N non valido** (151-154);
- Kyperounda Winery (2162): 3 nel 2024 (C/N 11,6);
- Botromagno (2269): 2 nel 2025 (C/N 8,6-8,9).

Fra Mirafiore e Tikves il limite «plausibile» non è un numero scelto dal proprietario: in questo report vale solo il caso
Tikves 2023. Se ne compaiono altri, chiedere.
