import ctypes, ctypes.wintypes, struct

RT_ICON       = 3
RT_GROUP_ICON = 14

k32 = ctypes.windll.kernel32

BeginUpdateResource = k32.BeginUpdateResourceW
BeginUpdateResource.restype  = ctypes.wintypes.HANDLE
BeginUpdateResource.argtypes = [ctypes.wintypes.LPCWSTR, ctypes.wintypes.BOOL]

UpdateResource = k32.UpdateResourceW
UpdateResource.restype  = ctypes.wintypes.BOOL
UpdateResource.argtypes = [ctypes.wintypes.HANDLE, ctypes.c_void_p, ctypes.c_void_p, ctypes.wintypes.WORD, ctypes.c_char_p, ctypes.wintypes.DWORD]

EndUpdateResource = k32.EndUpdateResourceW
EndUpdateResource.restype  = ctypes.wintypes.BOOL
EndUpdateResource.argtypes = [ctypes.wintypes.HANDLE, ctypes.wintypes.BOOL]

with open(r'resources\Tickr.ico', 'rb') as f:
    ico = f.read()

count = struct.unpack_from('<H', ico, 4)[0]
print('Patching with', count, 'images...')

h = BeginUpdateResource(r'out\result\Tickr.exe', False)
if not h:
    print('FAIL BeginUpdateResource')
    exit(1)

grp = bytearray(6 + count * 14)
struct.pack_into('<HHH', grp, 0, 0, 1, count)

for i in range(count):
    e = 6 + i * 16
    w, bh, cc, rv, pl, bc, sz, off = struct.unpack_from('<BBBBHHiI', ico, e)
    img = ico[off:off+sz]
    icon_id = i + 1
    ok = UpdateResource(h, RT_ICON, icon_id, 0, img, len(img))
    ge = 6 + i * 14
    struct.pack_into('<BBBBHHiH', grp, ge, w, bh, cc, rv, pl, bc, sz, icon_id)
    status = 'OK' if ok else 'FAIL'
    dim = str(w if w else 256) + 'x' + str(bh if bh else 256)
    print('  [' + str(i) + '] ' + dim + ' ' + str(bc) + 'bpp: ' + status)

UpdateResource(h, RT_GROUP_ICON, 1, 0, bytes(grp), len(grp))
result = EndUpdateResource(h, False)
print('Done:', 'SUCCESS' if result else 'FAIL')
