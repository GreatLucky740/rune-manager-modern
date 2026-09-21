"""Contour noir HUD Energy: sommets poses a la main sur grille x6."""
import os
import cv2
import numpy as np

OUT = r"C:\Users\Great-Lucky\Documents\Rune_Manager_Modern\Donnees\assets\croquis\hud-energy"

SRC = [
    (1, r"C:\Users\Great-Lucky\.cursor\projects\c-Users-Great-Lucky-Documents-Rune-Manager-Modern\assets\c__Users_Great-Lucky_AppData_Roaming_Cursor_User_workspaceStorage_bbc93fecd1b30cb2ac876d030bda9d68_images__875AD793-8EDB-4296-857B-180E21099953_-5b083c52-373f-4df9-9515-9c493e6f91d4.png",
     [(270, 145), (102, 340), (270, 524), (441, 340)]),
    (2, r"C:\Users\Great-Lucky\.cursor\projects\c-Users-Great-Lucky-Documents-Rune-Manager-Modern\assets\c__Users_Great-Lucky_AppData_Roaming_Cursor_User_workspaceStorage_bbc93fecd1b30cb2ac876d030bda9d68_images__4D607FFC-2538-4B70-A72F-B8B197D32EDB_-bcccddd3-edc5-48ca-9a7e-dc1da77d8b57.png",
     [(155, 180), (90, 330), (172, 435), (398, 435), (438, 195)]),
    (3, r"C:\Users\Great-Lucky\.cursor\projects\c-Users-Great-Lucky-Documents-Rune-Manager-Modern\assets\c__Users_Great-Lucky_AppData_Roaming_Cursor_User_workspaceStorage_bbc93fecd1b30cb2ac876d030bda9d68_images__735B7A02-0EEA-4E9B-857E-514E2BE6E42F_-96692dbf-457f-4e79-ae5f-696f9df90fb7.png",
     [(48, 250), (155, 182), (338, 188), (448, 438), (158, 442)]),
    (4, r"C:\Users\Great-Lucky\.cursor\projects\c-Users-Great-Lucky-Documents-Rune-Manager-Modern\assets\c__Users_Great-Lucky_AppData_Roaming_Cursor_User_workspaceStorage_bbc93fecd1b30cb2ac876d030bda9d68_images__617B5B67-0C49-44EB-967A-0BF0DF0C360D_-e0709474-dd6e-4501-a7d9-108ee575567f.png",
     [(158, 140), (105, 298), (282, 495), (465, 298), (412, 140)]),
    (5, r"C:\Users\Great-Lucky\.cursor\projects\c-Users-Great-Lucky-Documents-Rune-Manager-Modern\assets\c__Users_Great-Lucky_AppData_Roaming_Cursor_User_workspaceStorage_bbc93fecd1b30cb2ac876d030bda9d68_images__AA821781-A327-4EC2-8E0D-59560389CF11_-d18fd63c-bfbf-4771-8685-e8778298fed1.png",
     [(222, 188), (92, 388), (118, 435), (375, 435), (488, 295), (448, 188)]),
    (6, r"C:\Users\Great-Lucky\.cursor\projects\c-Users-Great-Lucky-Documents-Rune-Manager-Modern\assets\c__Users_Great-Lucky_AppData_Roaming_Cursor_User_workspaceStorage_bbc93fecd1b30cb2ac876d030bda9d68_images__0F6C3654-63F5-43BA-BCA0-F5066C0E53EE_-a41e61c5-9fe6-4083-9dfd-9b02d1ab467d.png",
     [(85, 178), (82, 315), (188, 435), (388, 435), (455, 292), (390, 180)]),
]

for slot, path, verts6 in SRC:
    bgr = cv2.imread(path)
    h, w = bgr.shape[:2]
    hull6 = np.array(verts6, np.int32).reshape(-1, 1, 2)
    orig = (np.array(verts6, np.float32) / 6.0).astype(np.int32)
    fill = np.zeros((h, w), np.uint8)
    cv2.fillConvexPoly(fill, orig.reshape(-1, 2), 255)
    vis = cv2.resize(bgr, (w * 6, h * 6), interpolation=cv2.INTER_NEAREST)
    cv2.polylines(vis, [hull6], True, (0, 255, 255), 3, cv2.LINE_AA)
    cv2.imwrite(os.path.join(OUT, "outline-slot%d.png" % slot), vis)
    np.save(os.path.join(OUT, "outline-slot%d.npy" % slot), fill > 0)
    print("slot", slot, "verts", len(verts6))
