# Color Palette

The application color palette is defined in [`src/Polisabroso.App/wwwroot/styles/tailwind.input.css`](../../src/Polisabroso.App/wwwroot/styles/tailwind.input.css). Use the semantic tokens below instead of introducing one-off colors in components.

| Token | Color | Intended use |
| --- | --- | --- |
| `primary` | `#4C001A` | Buttons, navigation, and important links |
| `primary-hover` | `#650024` | Hover states |
| `primary-active` | `#350012` | Pressed and active states |
| `primary-soft` | `#F3E8EE` | Badges, soft backgrounds, and selections |
| `background` | `#FCFAFB` | General page background |
| `surface` | `#FFFFFF` | Cards and modals |
| `text` | `#1F2937` | Main text |
| `text-muted` | `#66717E` | Secondary text |
| `border` | `#D7DCE2` | Borders and divisions |
| `success` | `#1F7A4C` | Success icons and text |
| `success-soft` | `#E9F7EF` | Background for success alerts |
| `warning` | `#9A6700` | Warning icons and text |
| `warning-soft` | `#FFF4CC` | Background for warning alerts |
| `error` | `#B42318` | Error icons and text |
| `error-soft` | `#FDECEA` | Background for error alerts |
| `info` | `#2563EB` | Information icons and text |
| `info-soft` | `#EFF6FF` | Background for information alerts |

When a component needs a color, choose the token whose semantic purpose matches the component state or content. Keep soft variants for alert or selection backgrounds, and use their corresponding solid variants for icons and text.
