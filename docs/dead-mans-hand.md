# Dead Man's Hand — complete noir investigation

Content version 2 turns the opening preview into a playable six-act campaign with ten distinct endings, fourteen defined inventory items and thirty case-board entries. Existing scene and adventure IDs remain valid.

## Investigation structure

| Act | Focus | Decisions and consequences |
| --- | --- | --- |
| I — Rain on Glass | Evelyn's commission and room 306 | Recover physical evidence; inspect multiple parts of the room; failed checks still advance the investigation. |
| II — The City Collects | Evelyn, the Ivory Room and the telephone exchange | Admission by invitation or deception; Moretti, Lena and the cashier offer different accounts; reconstruct the circuit with the clip and copied log. |
| III — Favors and Knives | River warehouses, photographer and pump house | Obtain independent freight records and the uncut negative; rescue Vane and Lena; use or improvise a dressing. |
| IV — The Badge Has Teeth | Hale, the impound archive and Rook | Separate Hale's complicity from Rook's orders; preserve the original ledger through evidence, procedure or a service-door entry. |
| V — Everyone Owes Someone | The Gazette, protected copies and witness transfer | Preserve independent duplicates, arrange protection or trade the original; accumulated attention makes the discreet witness transfer harder. |
| VI — Dead Man's Hand | The disposition of the case | Publication, prosecution, compromise, escape, blackmail, destruction, a narrow accusation, witness protection or an honest unresolved file. |

Earlier locations remain accessible until the player commits to the final decision. A failed die roll does not strand the run. Optional witnesses and documents are genuinely optional; stronger conclusions require the corresponding proof and protection rather than appearing for every run. Vane's participation in the redevelopment fraud is not erased by rescuing him. Hale can admit compliance without replacing Lena as the witness the player physically rescued.

## Items and use

Items appear in the persistent inventory and have contextual uses in story choices. Clicking an outlined inventory item opens the actions currently available for that object; keyboard Enter/Space supports the same action menu. The inventory scrolls within a bounded HUD area so a large case does not squeeze the story away.

| Item | Concrete use |
| --- | --- |
| Mirabelle duplicate key | Open the room's service cabinet for the circuit evidence. |
| Bent brass cufflink | Ask Moretti to identify the police clasp, contradicting Vane's presumed ownership. |
| Torn photograph | Earn Lena's account and identify the missing person in the picture. |
| Switchboard copy + bridge clip | Reconstruct the route and recover Rook's recorded order. |
| Ivory Room invitation | Gain admission without a deception roll. |
| Lockpick roll | Open either the warehouse or archive service door; the fine tools are expended. |
| Field dressing | Stabilize Vane without gambling his evacuation on an improvised treatment check; consumed once. |
| Shipping carbon | Preserve the archive exhibit and independently corroborate publication. |
| Uncut dated negative | Obtain Hale's account with photographic leverage. |
| Signed ledger | Preserve duplicates, submit the original, trade it, blackmail Rook or destroy it. |
| Exchange recording | Preserve the archive exhibit or corroborate publication. |
| Witness affidavit | Support prosecution/publication and arrange a protected transfer. |
| Two ferry berths | Evacuate the surviving Vane with Evelyn; consumed at departure. |

Purchases and document collection cannot be repeated to farm resources. Story choices use the existing engine effects and conditions. The attention counter affects the witness-transfer check (DC 13 or DC 16); failed transfer checks take a costly decoy route instead of silently deleting the witness.

## Endings (spoilers)

- **The Open Record:** independently corroborated public exposure with protected testimony.
- **Exhibit One:** the original and protected witness enter a formal prosecution.
- **The Paper Trail:** publish the established payments while stating the disappearance's evidentiary limits.
- **House Credit:** trade the original for Moretti's protection and accept the debt.
- **Two Berths Out:** Arthur survives and leaves with Evelyn; his signatures remain his responsibility.
- **The New Creditor:** turn the original into personal leverage inside Rook's institution.
- **The Cost of Mercy:** burn the original without an independent duplicate.
- **The Smaller Crime:** accuse Hale narrowly while leaving the structure above him intact.
- **A Living Witness:** prioritize a surviving witness over an unproven comprehensive case.
- **The File Stays Open:** refuse to manufacture a conclusion from missing proof.

## Save compatibility

`AdventureRunMigrations` upgrades older Vane runs in place. A completed `foundation-preview` at `foundation-checkpoint` becomes resumable in the library; the character, RunId, case board and completion-reward history remain intact. Already discovered opening objects are restored into inventory once. This is a content upgrade, not a second completion reward or a forced new game.

## Case-board correction

The large world Grid formerly sat directly inside a smaller Border. WPF could assign it a layout clip before applying the zoom RenderTransform, so zooming reduced an already clipped world into a small rectangle and hid cards at its right and bottom edges. A viewport-sized Canvas now arranges the entire world at origin (0,0); only the outer viewport clips the transformed scene.

Layout version 3 preserves stored card coordinates, clamps invalid edge positions, and repairs the old camera with a fit after the viewport is loaded. Zoom buttons use the viewport center; wheel zoom uses the pointer. The detail sidebar scrolls independently. Existing drag, selection, live connection movement and detached-story synchronization remain available. Newly created case boards group the opening evidence near the initial people.

## Validation

- Full unit suite: 531 passed, including all ten endings, proof gates, consumables, attention, failure paths, round-trip saves, graph reachability and preview migration.
- Release desktop and Windows smoke project build without warnings/errors.
- Windows runtime regression checks full node visibility and paper-colored rendered pixels at three sizes, the absence of a smaller pre-transform clip, retained custom positions, and detached-window live updates. It exports opening and complete-board captures.
