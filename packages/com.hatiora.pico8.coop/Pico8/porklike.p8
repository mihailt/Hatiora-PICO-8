pico-8 cartridge // http://www.pico-8.com
version 43
__lua__
-- porklike: wurst comes to worst
-- by krystian majewski, lazy devs

function _init()
    cartdata("porklike")
    ver = "v3"
    t, tani, buttbuff, fadetable, dirx, diry, dirpos, invdir, mob_ani, mob_type, mob_hp, mob_brain, brain_w, brain_a, mob_aspd, mob_plan, pick_name, pick_desc2d, crv_sig, crv_msk, dor_sig, dor_msk, free_sig, free_msk, wall_sig, wall_msk, tleaniadd, tleanisub, boomspr, spinspr, dirtle, voitle = 0, 0, 0, explode2d("0,0,0,0,0,0,0,0,0,0,0,0,0,0,0|1,1,129,129,129,129,129,129,129,129,0,0,0,0,0|2,2,2,130,130,130,130,130,128,128,128,128,128,0,0|3,3,3,131,131,131,131,129,129,129,129,129,0,0,0|4,4,132,132,132,132,132,132,130,128,128,128,128,0,0|5,5,133,133,133,133,130,130,128,128,128,128,128,0,0|6,6,134,13,13,13,141,5,5,5,133,130,128,128,0|7,6,6,6,134,134,134,134,5,5,5,133,130,128,0|8,8,136,136,136,136,132,132,132,130,128,128,128,128,0|9,9,9,4,4,4,4,132,132,132,128,128,128,128,0|10,10,138,138,138,4,4,4,132,132,133,128,128,128,0|11,139,139,139,139,3,3,3,3,129,129,129,0,0,0|12,12,12,140,140,140,140,131,131,131,1,129,129,129,0|13,13,141,141,5,5,5,133,133,130,129,129,128,128,0|14,14,14,134,134,141,141,2,2,133,130,130,128,128,0|15,143,143,134,134,134,134,5,5,5,133,133,128,128,0"), explodeval("-1,1,0,0,1,1,-1,-1"), explodeval("0,0,-1,1,-1,1,1,-1"), explodeval("-1,1,-18,18,-17,19,17,-19"), explodeval("2,1,4,3"), explode("164|165|166|167,128|129|128|130,134|135|136|137,138|139|138|140,131|132|131|133,141|142|143|142,144|145|146|147,187|188|189|190,151|152|153|152,154|154|154|154|154|154|154|154|154|154|154|154|155,156|156|156|157,158|158|158|159,160,161,162"), explode("pl,ai,ai,ai,ai,ai,ai,ai,we,ob,we,we,ob,ob,ob"), explodeval("5,1,1,1,2,1,1,10,1,1,1,1,1,1,1"), explodeval("1,2,4,2,2,2,5,6,3,1,1,1,1,1,1"), { ai_blank, ai_wait, ai_weed, ai_wait, ai_wait, ai_reaper }, { ai_blank, ai_attac, ai_weed, ai_queen, ai_kong, ai_reaper }, explodeval("1,2,2,2,1.8,1.5,2,1.5,1.5,3,1,1,1,1,1"), explode2d("2,2,2,2,2|2,2,3,2,2,3|2,2,3,2,2,3,2|4,4,4,5,4,4,4,5|4,4,5,4,4,5,4,4,5|5,4,5,4,4,5,4,4,5|6,6,6,7,6,6,6,7,6|6,6,7,6,6,7,6,6,7|6,7,6,7,6,7,6,7,6"), explode("jump,bolt,push,grapple,spear,smash,hook,spin,suplex,slap"), explode2d("jumps 2 spaces,stops enemies,you skip over|ranged attack,does 1 damage,and stops enemy|ranged attack,pushes enemy 1 space,and stops them|pulls yourself up to,the next occupied,space|hits 2 spaces in,any direction,|smashes a wall,or does 2 damage,|pulls an enemy,1 space towards you,and stops them|hits 4 spaces around,you,|lifts and throws,an enemy behind you,stops enemy|hits an enemy and,hops backwards,stops enemy", explode), explodeval("255,214,124,179,233"), explodeval("0,9,3,12,6"), explodeval("192,48"), explodeval("15,15"), explodeval("0,0,0,0,16,64,32,128,161,104,84,146"), explodeval("8,4,2,1,6,12,9,3,10,5,10,5"), explodeval("251,233,253,84,146,80,16,144,112,208,241,248,210,177,225,120,179,0,124,104,161,64,240,128,224,176,242,244,116,232,178,212,247,214,254,192,48,96,32,160,245,250,243,249,246,252"), explodeval("0,6,0,11,13,11,15,13,3,9,0,0,9,12,6,3,12,15,3,7,14,15,0,15,6,12,0,0,3,6,12,9,0,9,0,15,15,7,15,14,0,0,0,0,0,0"), explodeval("64,66,80"), explodeval("65,67,81"), explodeval("89,90,91,92"), explodeval("105,106,107,108"), explodeval("74,75,76"), explodeval("11,11,11,11,11,11,10")
    startgame()
end

function _update60()
    t += 1
    if buttbuff == 0 then
        for i = 1, 6 do
            if btnp(i - 1) then
                buttbuff = i
            end
        end
    end
    _upd()
    dohpwind()
end

function _draw()
    _drw()
    drawind()
    if logo_y > -24 then
        logo_t -= 1
        if logo_t <= 0 then
            logo_y += logo_t / 20
        end
        palt(11, true)
        palt(0, false)
        spr(210, 7, logo_y, 14, 3)
        oprint8("wurst comes to worst", 24, logo_y + 20, 7, 0)
        print(ver, 5, 154 - logo_y, 5)
    end
    if (floor == 0 or _upd == update_inv) and chain > 0 then
        print("wurstchain:" .. chain, 35, 119, chainsafe and 6 or 5)
    end
    if fadeperc > 0 then
        fadeperc = max(fadeperc - 0.04, 0)
        dofade()
    end
end

function startgame()
    poke(0x3101, 194)
    music "0"
    fadeperc, chain, chainsafe, buttbuff, logo_t, logo_y, hpwind, win, winfloor, turn, key, _upd, _drw, tmap, 😐, floats, wind, sprs, eqp, st_steps, st_kills = 1, dget(0), true, 0, 240, 35, nil, false, 10, "ai", 0, update_anis, draw_game, blankmap(1), addmob(1, 20), {}, {}, {}, {}, 0, 0
    genfloor(0)
end
-->8
--updates
function update_game()
    if buttbuff == 0 then return end
    logo_t = min(logo_t, 0)
    if buttbuff < 5 then
        thrdir = buttbuff
        moveplayer(buttbuff)
    elseif buttbuff == 6 then
        showinv()
        sfx "3"
    end
    buttbuff = 0
end
function update_inv()
    if buttbuff == 3 or buttbuff == 4 then
        sfx "5"
        invwind.cur -= diry[buttbuff + 1]
    end
    invwind.cur = (invwind.cur - 1) % #invwind.txt + 1
    sel = invwind.cur
    seleqp, buttbuff = eqp[sel], 0
    updatedesc()
    if btnp "4" then
        sfx "2"
        _upd = update_game
        hideinv()
    elseif btnp "5" then
        if seleqp == nil then
            sfx "9"
        else
            _upd, drawarr, invwind.invert, invwind.col[sel] = update_targ, true, true, 0
            if tmap[😐.pos + dirpos[thrdir]] < 0 then
                thrdir = invdir[thrdir]
            end
            sfx "3"
            hideinv()
            local txt = "  " .. pick_name[seleqp.typ]
            local w = #txt * 4 + 7
            usewind = addwind(64 - w / 2, hpwind.y, w, 13, { txt })
            usewind.icn, hpwind.dur, hpwind = { seleqp.typ }, 0, nil
        end
    end
end

function update_targ()
    usewind.y = hpwind.y
    if buttbuff > 0 and buttbuff < 5 then
        thrdir = buttbuff
        mobdir(😐, thrdir)
        sfx "5"
    end
    if btnp "4" then
        drawarr = false
        showinv()
        invwind.cur, usewind.dur, usewind = sel, 0, nil
        sfx "2"
    elseif btnp "5" then
        sfx "3"
        eat()
        drawarr, usewind.dur, usewind = false, 0, nil
    end
    buttbuff = 0
end

function update_anis()
    local pass = #sprs == 0
    for arr in all({ mobs, picks }) do
        for m in all(arr) do
            if m.mov then
                m.t, pass = min(m.t + m.dt, 1), false
                m:mov()
                checkend(m)
            end
        end
    end
    if pass then
        passturn()
    end
end

function passturn()
    if turn == "player" then
        if noturn then
            _upd, noturn = update_game, false
        else
            turn = "ai"
            doai(false)
        end
    elseif turn == "ai" then
        turn = "weeds"
        doai(true)
    elseif turn == "weeds" then
        if win or 😐.hp <= 0 then
            showgover()
            return
        end
        if steps == 100 and floor > 0 then
            steps += 1
            sfx "15"
            showmsg("wurstlord awakens")
            addmob(8, dropspot(spawnpos))
        end
        turn, _upd = "player", update_game
        if 😐.recover > 0 then
            😐.recover -= 1
            if 😐.recover == 0 then
                😐.sight = 4
                unfog()
            end
        end
    end
end

function update_gover()
    if btnp "5" then
        sfx "3"
        fadeout()
        startgame()
    end
end
-->8
--draws
function draw_game()
    cls "0"
    if fadeperc == 1 then return end
    tani += 1
    if tani >= 7 then
        tani = 0
        for p = 20, 305 do
            local tle = tmap[p]
            if find(tleaniadd, tle) then
                tle += 1
            elseif find(tleanisub, tle) then
                tle -= 1
            end
            tmap[p] = tle
        end
    end
    for x = 0, 15 do
        for y = 0, 15 do
            local pos = xytopos(x + 1, y + 1)
            if fog[pos] == 0 then
                spr(tmap[pos], x * 8, y * 8)
            end
        end
    end
    for p in all(picks) do
        if fog[p.pos] == 0 then
            local x, y = postoscreen(p.pos)
            myspr(getframe(p.ani, 4), x + p.ox, y + p.oy)
            if p.hop then
                --t/24*4)/8
                y = y - sin(t / 48 - 0.1)
                spr(p.typ + 114, x + p.ox + 0.5, y + p.oy + 0.5)
            end
        end
    end
    for m in all(dmobs) do
        drawmob(m)
        m.dur -= 1
        if m.dur <= 0 then
            del(dmobs, m)
        end
    end
    for i = #mobs, 1, -1 do
        drawmob(mobs[i])
    end
    for s in all(sprs) do
        s.t = s.t + 1
        if s.t >= s.maxt then
            del(sprs, s)
            if s.trg_mob then
                s.trg_func(s.trg_mob, s.trg_val, s.trg_val2)
            end
        elseif s.t >= 0 then
            if s.dx then
                s.x += s.dx
                s.y += s.dy
                if s.drag then
                    s.dx *= s.drag
                    s.dy *= s.drag
                end
            end
            myspr(s.ani[flr(#s.ani * s.t / s.maxt) + 1], s.x, s.y, s.flx, s.fly)
        end
    end
    for f in all(floats) do
        oprint8(f.txt, f.x, max(f.y, 0), f.c == 8 and getframe({ 8, 14, 7 }, 4) or f.c, 0)
        f.y += (f.ty - f.y) / 10
        f.t += 1
        if f.t > 70 then
            del(floats, f)
        end
    end
    if drawarr then
        if seleqp.typ == 8 then
            for i = 1, 4 do
                drawarrow(i)
            end
        else
            drawarrow(thrdir)
        end
    end
end

function drawarrow(dr)
    local x, y = postoscreen(😐.pos + dirpos[dr])
    myspr(
        dr + 170,
        x + sin(t / 15) * dirx[dr] + dirx[dr],
        y + sin(t / 15) * diry[dr] + diry[dr]
    )
end

function drawmob(m)
    if fog[m.pos] != 0 or m.vis == false then return end
    if m.flash > 0 then
        m.flash -= 1
        pal(10, 7)
    end
    if m.haskey and sin(t / 24) > 0.8 then
        pal(10, 7)
    end
    local x, y = postoscreen(m.pos)
    myspr(getframe(m.ani, m.aspd), x + m.ox, y + m.oy, m.flp)
end

function myspr(sp, x, y, flp, flpy)
    palt(11, true)
    palt(0, false)
    spr(sp, x + 0.5, y + 0.5, 1, 1, flp, flpy)
    pal()
end

function draw_gover()
    cls()
    if win then
        print("the kielbasa is yours", 23, 47, 10)
        spr(77, 51, 15, 3, 3)
    else
        print("you died", 48, 47, 6)
        spr(168, 51, 15, 3, 3)
    end
    print("press ❎", 48, 100, getframe({ 5, 6, 7 }, 1.5))
end

-->8
--tools
function getframe(ani, spd)
    return ani[flr(t / 24 * spd) % #ani + 1]
end

function find(arr, val)
    if val == nil then return false end
    for v in all(arr) do
        if v == val then return true end
    end
    return false
end

function oprint8(_t, _x, _y, _c, _c2)
    for i = 1, 8 do
        print(_t, _x + dirx[i], _y + diry[i], _c2)
    end
    print(_t, _x, _y, _c)
end

function dist(pos1, pos2)
    local fx, fy = postoxy(pos1)
    local tx, ty = postoxy(pos2)
    local dx, dy = fx - tx, fy - ty
    return sqrt(dx * dx + dy * dy)
end

function mysgn(x)
    return x > 0 and 1 or x < 0 and -1 or 0
end

function los(pos1, pos2, retest)
    if dist(pos1, pos2) == 1 then return true end
    local x1, y1 = postoxy(pos1)
    local x2, y2 = postoxy(pos2)
    local dx, dy, sx, sy = abs(x2 - x1), abs(y2 - y1), sgn(x2 - x1), sgn(y2 - y1)
    local err, e2, frst = dx - dy, true
    while not (x1 == x2 and y1 == y2) do
        if not frst and isw(xytopos(x1, y1), "sight") == false then
            if retest then
                return false
            end
            return los(pos2, pos1, true)
        end
        e2, frst = err + err, false
        if e2 > -dy then
            err -= dy
            x1 += sx
        end
        if e2 < dx then
            err += dx
            y1 += sy
        end
    end
    return true
end

function dofade()
    fadeperc = min(fadeperc, 1)
    for c = 0, 15 do
        pal(c, fadetable[c + 1][flr(fadeperc * 16 + 1)], 1)
    end
end

function wait(_wait)
    repeat
        _wait -= 1
        flip()
    until _wait < 0
end

function fadeout(spd, _wait)
    local spd, _wait = spd or 0.04, _wait or 0
    repeat
        fadeperc = min(fadeperc + spd, 1)
        dofade()
        flip()
    until fadeperc == 1
    wait(_wait)
end

function blankmap(_dflt)
    --offmap is -1
    local ret, _dflt = {}, _dflt or 0

    for x = 0, 17 do
        for y = 0, 17 do
            ret[xytopos(x, y)] = (x == 0 or x == 17 or y == 0 or y == 17) and -1 or _dflt
        end
    end
    return ret
end

function postoxy(pos)
    return (pos - 1) % 18, flr((pos - 1) / 18)
end

function postoscreen(pos)
    local x, y = postoxy(pos)
    return (x - 1) * 8, (y - 1) * 8
end

function xytopos(x, y)
    return 1 + x + y * 18
end

function getrnd(arr)
    return arr[1 + flr(rnd(#arr))]
end

function copymap(x, y)
    for _x = 0, 15 do
        for _y = 0, 15 do
            local tle, pos = mget(_x + x, _y + y), xytopos(_x + 1, _y + 1)
            if tle == 15 or tle == 182 then
                😐.pos, spawnpos = pos, pos
            elseif tle == 14 then
                epos = pos
            elseif tle == 63 then
                tle, gpos = 14, pos
            elseif tle == 68 then
                tle, ♥pos = 72, pos
            end
            tmap[pos] = tle
        end
    end
end

function explode(s, sep)
    local sep, retval, lastpos = sep or ",", {}, 1
    for i = 1, #s do
        if sub(s, i, i) == sep then
            add(retval, sub(s, lastpos, i - 1))
            i += 1
            lastpos = i
        end
    end
    add(retval, sub(s, lastpos, #s))
    return retval
end

function explode2d(s, func)
    local arr, func = explode(s, "|"), func or explodeval
    for i = 1, #arr do
        arr[i] = func(arr[i])
    end
    return arr
end

function explodeval(_arr, sep)
    return toval(explode(_arr, sep))
end

function toval(_arr)
    local _retarr = {}
    for _i in all(_arr) do
        add(_retarr, flr(tonum(_i)))
    end
    return _retarr
end

function calcdist(tpos, mode)
    local mode, cand, step, candnew = mode or "test", {}, 0
    distmap = blankmap(-2)
    --8140
    if mode == "dist" then
        for p = 20, 305 do
            distmap[p] = dist(tpos, p)
        end
        return
    end
    add(cand, tpos)
    distmap[tpos] = 0
    repeat
        step += 1
        candnew = {}
        for c in all(cand) do
            for d = 1, 4 do
                local npos = c + dirpos[d]
                if distmap[npos] == -2 then
                    distmap[npos] = step
                    if isw(npos, mode) then
                        add(candnew, npos)
                    end
                end
            end
        end
        cand = candnew
    until #cand == 0
end
-->8
--gameplay
function moveplayer(dr)
    local destpos = 😐.pos + dirpos[dr]
    if isw(destpos, "checkmobs") then
        mobwalk(😐, dr)
        😐.trig = true
        sfx "12"
        st_steps += 1
        steps += 1
    else
        local mb, spd = getmob(destpos), nil
        if mb then
            hitmob(mb, 1)
            if mb.typ == 4 and rnd(2) < 1 then
                blind(😐)
            end
        elseif fget(tmap[destpos], 1) then
            if bumptile(destpos) then
                sfx "19"
            else
                spd, noturn = 3, true
            end
        else
            spd, noturn = 3, true
        end
        mobbump(😐, dr, spd)
        😐.trig = spd == nil
    end
    _upd = update_anis
end

function bumptile(pos)
    local tle = tmap[pos]
    if tle == 97 or tle == 99 then
        tmap[pos] += 1
    elseif tle == 82 or tle == 114 then
        tmap[pos] = 115
        resetneighs(pos)
    elseif tle == 63 then
        if key < 1 then
            showmsg("one monster holds the key")
            sfx "3"
            return false
        else
            key -= 1
            tmap[pos] = 1
        end
    end
    return true
end

function trig_step(mob, skip7)
    local pos = mob.pos
    local tle = tmap[pos]

    if mob == 😐 then
        unfog()
        if tle == 14 then
            sfx "4"
            fadeout()
            😐.recover = 1
            genfloor(floor + 1)
            poke(0x5f95, floor)
            return
        elseif tle == 110 then
            win = true
            return
        end

        local p = getpick(pos)
        if p then
            pickup(p)
        end

        local vpos = voidmap[pos]
        if vpos > 0 then
            local pos2 = voids[vpos].exit
            if pos2 and fget(tmap[pos2], 6) then
                tmap[pos2] += 65
            end
        end
    end
    if tle == 81 or tle == 80 then
        hitmob(mob, 3)
        if mob.hp <= 0 and mob.ai then
            droppick_rnd(mob.pos)
        end
        tmap[pos] = 176
    elseif tle == 7 and pos != mob.last7 then
        teleport(mob)
        mob.last7 = mob.pos
        trig_step(mob)
    end

    if tle != 7 then
        mob.last7 = -1
    end
end

function getmob(pos)
    for m in all(mobs) do
        if m.pos == pos then
            return m
        end
    end
    return false
end

function noneighs(pos)
    for i = 1, 4 do
        if getmob(pos + dirpos[i]) then
            return false
        end
    end
    return true
end

function pickup(pick)
    local s = 10
    pick.typ -= 1
    local i, typ, fre, flt = 0, pick.typ
    if typ == -1 then
        😐.hp += 1
        flt, s = "+1", 20
    elseif typ == 0 then
        key += 1
        flt = "key"
    else
        flt = pick_name[typ]
        for i = 1, 4 do
            local e = eqp[i]
            if fre == nil and e == nil then
                fre = i
            elseif e and e.typ == typ then
                e.chrg += pick.chrg
                typ = 99
            end
        end
        if typ != 99 then
            if fre then
                eqp[fre] = pick
            else
                addfloat("full!", 😐.pos, 9)
                pick.typ += 1
                sfx "9"
                return
            end
        end
    end
    del(picks, pick)
    addfloat(flt, 😐.pos, 7)
    sfx(s)
end

function getpick(pos)
    for p in all(picks) do
        if p.pos == pos then
            return p
        end
    end
    return false
end

function isw(pos, mode)
    local mode, tle = mode or "test", tmap[pos] or -1

    if mode == "sight" then
        return not fget(tle, 2)
    else
        if tle > 0 and not fget(tle, 0) then
            if mode == "checkmobs" then
                return not getmob(pos)
            elseif mode == "smart" then
                return not fget(tle, 1)
            elseif mode == "smartmob" or mode == "ai" then
                if not fget(tle, 1) then
                    local m = getmob(pos)
                    if m and mode == "ai" then
                        return m.active == true
                    end
                    return not m
                end
                return false
            end
            return true
        end
    end
    return false
end

function hitpos(pos, dmg, stun)
    hitmob(getmob(pos), dmg, stun)
    local tle = tmap[pos]
    if tle == nil then
        return
    elseif tle == 80 or tle == 81 then
        tle = 176
        sfx "18"
    elseif tle > 176 and fget(tle, 3) then
        tle = getrnd(dirtle)
        resetneighs(pos)
        sfx "18"
    end
    tmap[pos] = tle
    unfog()
end

function hitmob(mob, dmg, stun)
    if not mob then return end
    if mob.ob then
        openob(mob)
        return
    end
    mob.hp -= dmg
    mob.flash, mob.stun = 10, mob.stun or stun == true
    addfloat("-" .. dmg, mob.pos, mob == 😐 and 8 or 9)

    sfx(mob == 😐 and 6 or 7)
    mob.vis = true

    if mob.hp <= 0 then
        del(mobs, mob)
        add(dmobs, mob)
        mob.dur, mob.mov = 7, nil

        if mob.haskey then
            droppick(1, mob.pos)
        end
        if mob.typ == 8 then
            😐.hp = max(😐.hp, 5)
            sfx "20"
            droppick(1, mob.pos)
            poke(0x5f96, 1)
        end
        if mob.ai then
            st_kills += 1
        end
    end
end

function openob(ob)
    local sx, perc, tle, slime, pos = 8, 20, dirt(ob.pos), true, ob.pos
    del(mobs, ob)
    if ob.typ == 15 then
        sx, perc, tle, slime = 19, 100, 9, false
        if floor == 0 then
            chainsafe = false
            dset(0, 0)
        end
    elseif ob.typ == 10 then
        sx, perc, tle, slime = 14, 100, 73, false
        boom(pos)
    end

    sfx(sx)
    if not fget(tmap[pos], 1) then
        tmap[pos] = tle
    end
    if rnd(100) < perc then
        if slime and rnd(5) < 1 then
            sfx "9"
            local p = dropspot(pos)
            mobhop(addmob(2, pos), p)
        elseif ob.♥ and rnd(5) < 1 then
            droppick(0, pos)
        else
            droppick_rnd(pos)
        end
    end
end

function boom(pos)
    for i = 1, 8 do
        local p = pos + dirpos[i]
        hitpos(p, 1)
        local tle = tmap[p]
        if fget(tle, 5) then
            tle = 5
        elseif isw(p) and not fget(tle, 1) then
            tle = getrnd(dirtle)
        end
        tmap[p] = tle
    end
    local boomx, boomy = postoscreen(pos)
    for i = 0, 20 do
        local ang, dist, spd = rnd(), rnd(5), 0.5 + rnd(0.5)
        add(
            sprs, {
                ani = boomspr,
                t = 0,
                maxt = 10 + rnd(35),
                x = boomx + sin(ang) * dist,
                y = boomy + cos(ang) * dist,
                dx = sin(ang) * spd,
                dy = cos(ang) * spd,
                drag = 0.9
            }
        )
    end
end

function sight(mb, pos)
    return dist(mb.pos, pos) <= mb.sight
            and los(mb.pos, pos)
end

function unfog()
    for p = 20, 305 do
        if fog[p] == 1 and dist(😐.pos, p) <= 😐.sight and los(😐.pos, p) then
            unfogtile(p)
        end
    end
end

function unfogtile(pos)
    fog[pos] = 0
    if isw(pos, "sight") then
        for i = 1, 4 do
            local npos = pos + dirpos[i]
            if not isw(npos) then
                fog[npos] = 0
            end
        end
    end
end

function showgover()
    if win then
        music "24"
        chain += 1
        dset(0, chain)
        poke(0x5f97, chain)
    else
        music "22"
    end
    wait(70)
    fadeout(0.02)
    _upd, _drw, wind = update_gover, draw_gover, {}

    local wnd = addwind(
        40, 60, 53, 31, {
            "floor: " .. floor,
            "steps: " .. st_steps,
            "kills: " .. st_kills
        }
    )
    wnd.noborder, wnd.col = true, { 5, 5, 5 }
end

function eat()
    local abl, pos, tdir = seleqp.typ, 😐.pos, dirpos[thrdir]
    local pos1, pos2, posb, hit, land = pos + tdir, pos + tdir * 2, pos - tdir, throwtile(pos, thrdir)
    local mob1, mob2, mobh = getmob(pos1), getmob(pos2), getmob(hit)

    if abl == 1 then
        if isw(pos2, "checkmobs") then
            if mob1 then
                mob1.stun = true
            end
            mobhop(😐, pos2)
            sfx "11"
        else
            sfx "21"
        end
    elseif abl == 2 then
        sfx "11"
        local sp = shoot(pos, hit)
        sp.trg_func, sp.trg_mob, sp.trg_val, sp.trg_val2 = hitpos, hit, 1, true
    elseif abl == 3 then
        sfx "11"
        local sp = shoot(pos, hit)
        sp.trg_val, sp.trg_func, sp.trg_mob = thrdir, mobpush, mobh
    elseif abl == 4 then
        local sp = rope(pos, land)
        sp.trg_val, sp.trg_func, sp.trg_mob = land, mobhop, 😐
        sfx "11"
    elseif abl == 5 then
        rope(pos, pos2)
        sfx "17"
        hitpos(pos1, 1)
        hitpos(pos2, 1)
        mobbump(😐, thrdir)
    elseif abl == 6 then
        mobbump(😐, thrdir)
        hitpos(pos1, 2)
        if fget(tmap[pos1], 7) then
            tmap[pos1] = getrnd(dirtle)
            resetneighs(pos1)
            sfx "18"
        end
    elseif abl == 7 then
        local sp = rope(pos, hit)
        sfx "11"
        sp.trg_func, sp.trg_mob, sp.trg_val = mobpush, mobh, invdir[thrdir]
    elseif abl == 8 then
        sfx "16"
        for i = 1, 4 do
            local posx, posy = postoscreen(pos)
            add(
                sprs, {
                    ani = spinspr,
                    t = i * -4,
                    maxt = 7,
                    x = posx,
                    y = posy,
                    dx = dirx[i] * 2,
                    dy = diry[i] * 2,
                    drag = 0.9,
                    trg_func = hitpos,
                    trg_mob = pos + dirpos[i],
                    trg_val = 1
                }
            )
        end
        mobhop(😐, pos)
    elseif abl == 9 then
        if mob1 and isw(posb, "checkmobs") then
            mobhop(mob1, posb)
            sfx "11"
            mob1.stun, mob1.trig = true, true
            mobbump(😐, thrdir)
        else
            sfx "21"
        end
    elseif abl == 10 then
        if isw(posb, "checkmobs") then
            mobhop(😐, posb)
            sfx "11"
        else
            mobbump(😐, invdir[thrdir])
        end
        hitpos(pos1, 1, true)
    end
    eqp[sel].chrg -= 1
    if eqp[sel].chrg <= 0 then
        eqp[sel] = nil
    end
    _upd = update_anis
    😐.trig = true
    unfog()
end

function throwtile(pos, dr)
    local d = dirpos[dr]
    repeat
        pos += d
    until not isw(pos, "checkmobs")
    return pos, pos - d
end

function mobpush(mb, dr)
    if mb then
        if isw(mb.pos + dirpos[dr], "checkmobs") then
            mobwalk(mb, dr)
            mb.trig = true
        else
            mobbump(mb, dr)
        end
        mb.stun, mb.vis = true, true
        sfx "13"
    end
end

function resetneighs(pos)
    local tle = tmap[pos + 18]
    if tle then
        if tle == 4 or tle == 180 then
            tle = 1
        elseif tle == 5 then
            tle = getrnd(dirtle)
        elseif tle == 6 then
            tle = 12
        end
        tmap[pos + 18] = tle
    end
end

function dirt(pos)
    return (pos < 36 or fget(tmap[pos - 18], 7)) and 5 or getrnd(dirtle)
end

function teleport(mob)
    for p = 20, 305 do
        if tmap[p] == 7 and isw(p, "checkmobs") then
            mob.pos = p
            sfx "1"
            return
        end
    end
end

function blind(mob)
    if mob.sight > 1 then
        mob.sight, mob.recover = 1, 30
        if mob == 😐 then
            sfx "0"
            addfloat("blind", 😐.pos, 9)
            fog = blankmap(1)
            unfog()
        end
    end
end
-->8
--ui

function addfloat(_txt, pos, _c)
    local _x, _y = postoscreen(pos)
    _x += 4 - #_txt * 2
    add(floats, { txt = _txt, x = _x, y = _y, c = _c, ty = _y - 10, t = 0 })
end

function addwind(_x, _y, _w, _h, _txt)
    local w = {
        x = _x,
        y = _y,
        w = _w,
        h = _h,
        txt = _txt
    }
    add(wind, w)
    return w
end

function drawind()
    for w in all(wind) do
        local wx, wy, ww, wh = w.x, w.y, w.w, w.h
        rectfill(wx, wy, wx + ww - 1, wy + wh - 1, 0)
        if w.noborder != true then rect(wx + 1, wy + 1, wx + ww - 2, wy + wh - 2, 6) end
        wx += 4
        wy += 4
        clip(wx, wy - 1, ww - 8, wh - 6)
        if w.cur then
            wx += 6
        end
        for i = 1, #w.txt do
            local txt, c = w.txt[i], 6
            if i == w.cur then
                spr(175, wx - 4.5 + sin(time()), wy)
                if w.invert then
                    rectfill(wx - 1, wy - 1, wx + #txt * 4, wy + 5, 7)
                end
            end
            if w.col and w.col[i] then
                c = w.col[i]
            end
            if w.icn and w.icn[i] then
                pal(10, w.invert and 0 or 10)
                spr(w.icn[i] + 115, wx, wy)
                pal()
            end
            print(txt, wx, wy, c)
            wy += 6
        end

        clip()
        if w.dur then
            w.dur -= 1
            if w.dur <= 0 then
                local dif = w.h / 4
                w.y += dif / 2
                w.h -= dif
                if w.h < 3 then
                    del(wind, w)
                end
            end
        end
    end
end

function showmsg(txt)
    local wid = (#txt + 2) * 4 + 7
    local w = addwind(63 - wid / 2, 50, wid, 13, { " " .. txt })
    w.dur = 120
end

function dohpwind()
    local hpy = 😐.pos < 161 and 110 or 5
    hpwind = hpwind or addwind(5, hpy, 23, 13, {})
    if _upd == update_inv then hpy = 5 end
    hpwind.txt[1] = "♥" .. 😐.hp
    hpwind.y += (hpy - hpwind.y) / 5
end

function showinv()
    local txt, col, icn = {}, {}, {}
    _upd = update_inv
    for i = 1, 4 do
        if eqp[i] then
            add(txt, "  " .. pick_name[eqp[i].typ] .. " (" .. eqp[i].chrg .. ")")
            add(col, 6)
            icn[i] = eqp[i].typ
        else
            add(txt, "...")
            add(col, 5)
        end
    end

    invwind = addwind(5, 17, 88, 31, txt)
    invwind.cur, invwind.col, invwind.icn = 1, col, icn
    keywind = addwind(27, 5, 20, 13, { "  " .. key })
    keywind.icn = { 11 }
    flrwind = addwind(46, 5, 47, 13, { "floor " .. floor })
    if 😐.sight == 1 then
        flrwind.txt[1] = "blind ⧗" .. 😐.recover
    end
    updatedesc()
end

function hideinv()
    invwind.dur, keywind.dur, flrwind.dur = 5, 5, 5
    if descwind then
        descwind.dur, descwind = 0, nil
    end
end

function updatedesc()
    if seleqp then
        if not descwind then
            descwind = addwind(5, 47, 88, 37, {})
        end
        local et = seleqp.typ
        descwind.txt = {
            pick_name[et], "",
            pick_desc2d[et][1],
            pick_desc2d[et][2],
            pick_desc2d[et][3]
        }
    else
        if descwind then
            descwind.dur, descwind = 0, nil
        end
    end
end

function shoot(from, to, sprite, flipx, flipy)
    local fx, fy = postoscreen(from)
    local d, sprite, tx, ty = dist(from, to) * 8, sprite or 84, postoscreen(to)
    local sp = {
        ani = { sprite },
        t = 0,
        maxt = d / 3,
        x = fx,
        y = fy,
        dx = mysgn(tx - fx) * 3,
        dy = mysgn(ty - fy) * 3,
        flx = flipx,
        fly = flipy
    }
    add(sprs, sp)
    return sp
end

function rope(from, to)
    local fx, fy = postoscreen(from)
    local tx, ty = postoscreen(to)
    if fx != tx and fy != ty then
        return
    end
    local d, tip, tail, dirx, diry = dist(from, to) * 8, 87, 88, mysgn(tx - fx), mysgn(ty - fy)
    if ty != fy then
        tip, tail = 85, 101
    end
    local tax, tay, tat = fx + dirx * 4, fy + diry * 4, -4 / 3
    repeat
        local sp = {
            ani = { tail },
            t = tat,
            maxt = d / 3 + tat,
            x = tax,
            y = tay
        }
        add(sprs, sp)
        tax += dirx * 8
        tay += diry * 8
        tat -= 8 / 3
    until abs(tax - tx) < 8 and abs(tay - ty) < 8
    return shoot(from, to, tip, tx > fx, ty > fy)
end

-->8
--mobs and items

function addmob(_typ, _pos)
    local mty = mob_type[_typ]
    local m = {
        typ = _typ,
        pos = _pos,
        ox = 0,
        dt = 0.125,
        oy = 0,
        flp = false,
        flash = 0,
        aspd = mob_aspd[_typ],
        oaspd = mob_aspd[_typ],
        sight = 4,
        aicool = 0,
        charge = 0,
        recover = 0,
        vis = true,
        hp = mob_hp[_typ],
        ani = explodeval(mob_ani[_typ], "|"),
        task = brain_w[mob_brain[_typ]],
        taska = brain_a[mob_brain[_typ]],
        ai = mty == "ai" or mty == "we",
        ob = mty == "ob",
        we = mty == "we"
    }
    add(mobs, m)
    return m
end

function addpick(_typ, _pos)
    local p = {
        typ = _typ,
        dt = 0.07,
        pos = _pos,
        chrg = 2,
        ox = 0,
        oy = 0,
        ani = explodeval("96,112,113,112,96,96,96,96"),
        hop = _typ > 1
    }

    if _typ == 0 then
        p.ani = explodeval("68,69,68,70,68,68,68,68")
    elseif _typ == 1 then
        p.ani = explodeval("102,103,102,104,102,102,102,102")
    end
    add(picks, p)
    return p
end

function checkend(mb)
    if mb.t >= 1 then
        mb.mov, mb.aspd = nil, mb.oaspd
        if mb.trig then
            mb.trig = false
            trig_step(mb)
        end
        if mb.pounce then
            mb.pounce = false
            ai_dobump(mb)
        end
    end
end

function mobwalk(mb, dr)
    mobdir(mb, dr)
    mb.pos += dirpos[dr]
    mb.sox, mb.soy, mb.mov, mb.aspd, mb.t = -dirx[dr] * 8, -diry[dr] * 8, mov_walk, 6, 0
    mb.ox, mb.oy = mb.sox, mb.soy
end

function mobbump(mb, dr, dst)
    local dst = dst or 8
    mobdir(mb, dr)
    mb.sox, mb.soy, mb.ox, mb.oy, mb.mov, mb.t = dirx[dr] * dst, diry[dr] * dst, 0, 0, mov_bump, 0
end

function mobhop(mb, to)
    local fx, fy = postoscreen(mb.pos)
    local tx, ty = postoscreen(to)
    mb.sox, mb.soy, mb.mov, mb.hoph, mb.t, mb.pos = fx - tx, fy - ty, mov_hop, 4, 0, to
    mb.ox, mb.oy = mb.sox, mb.soy
end

function mobdir(mb, dr)
    mb.flp = dirx[dr] == 0 and mb.flp or dirx[dr] < 0
end

function mov_walk(self)
    local tme = 1 - max(self.t, 0)
    self.ox, self.oy = self.sox * tme, self.soy * tme
end

function mov_bump(self)
    local mt = max(self.t, 0)
    local tme = mt > 0.5 and 1 - mt or mt
    self.ox, self.oy = self.sox * tme, self.soy * tme
end

function mov_hop(self)
    local tme = 1 - max(self.t, 0)
    self.ox, self.oy = self.sox * tme, self.soy * tme + sin(tme / 2) * self.hoph
end

function doai(wee)
    local moving = false

    for m in all(mobs) do
        if m != 😐 and m.we == wee then
            if m.stun then
                m.stun = false
            else
                moving = m.task(m) or moving
            end
        end
    end

    if moving then
        _upd = update_anis
    else
        passturn()
    end
end

function ai_wait(m)
    if sight(m, 😐.pos) then
        m.task, m.tpos, m.aicool, m.active, buttbuff = m.taska, 😐.pos, 0, true, 0
        addfloat("!", m.pos, 10)
    end
    return false
end

function ai_blank()
    return false
end

function ai_weed(m)
    for i = 1, 4 do
        local mb = getmob(m.pos + dirpos[i])
        if mb then
            mobbump(m, i)
            hitmob(mb, 1)
            return true
        end
    end
    return false
end

function ai_dobump(m)
    local d = 5
    if dist(m.pos, 😐.pos) == 1 then
        repeat
            d -= 1
        until dirpos[d] == 😐.pos - m.pos or d == 1
        mobbump(m, d)
        hitmob(😐, 1)
        if m.typ == 4 then
            blind(😐)
        end
        return true
    end
    return false
end

function ai_tcheck(m)
    m.aicool += 1
    if sight(m, 😐.pos) then
        m.tpos, m.aicool = 😐.pos, 0
    end
    if m.pos == m.tpos or m.aicool > 8 then
        m.task, m.active = ai_wait, false
        addfloat("?", m.pos, 10)
        return false
    end
    return true
end

function ai_attac(m)
    m.vis = true
    if ai_dobump(m) then
        return true
    elseif ai_tcheck(m) then
        local cand = getnextstep(m.pos, m.tpos)
        if cand > 0 then
            mobwalk(m, cand)
            if m.typ == 6 and dist(m.pos, 😐.pos) > 1 and rnd(3) > 1 then
                m.vis = false
            end
            m.trig = true
            return true
        end
    end
    return false
end

function ai_kong(m)
    if ai_dobump(m) then
        return true
    elseif ai_tcheck(m) then
        local cand, cand2, d2 = getnextstep(m.pos, m.tpos)

        local px, py = postoxy(m.tpos)
        local mx, my = postoxy(m.pos)
        if mx == px or my == py then
            for i = 1, 4 do
                local pos = m.pos + dirpos[i]
                while cand2 == nil and pos != m.tpos and isw(pos) do
                    if isw(pos, "smartmob") and distmap[pos] == 1 then
                        cand2, d2 = pos, i
                    end
                    pos += dirpos[i]
                end
            end
        end

        if cand2 then
            mobdir(m, d2)
            mobhop(m, cand2)
            m.trig, m.pounce = true, true
            sfx "1"
            return true
        else
            if cand > 0 then
                mobwalk(m, cand)
                m.trig = true
                return true
            end
        end
    end
    return false
end

function ai_reaper(m)
    if ai_dobump(m) then
        return true
    else
        local cand = getnextstep(m.pos, 😐.pos)
        if cand > 0 then
            mobwalk(m, cand)
            m.trig = true
            return true
        end
    end
    return false
end

function getnextstep(from, to, rev, mode)
    local bdst, cand, mode = 999, 0, mode or "ai"
    calcdist(to, mode)
    for i = 1, 4 do
        local npos = from + dirpos[i]
        if isw(npos, "smartmob") then
            local dst = distmap[npos] + rnd()
            if dst > 0 then
                if rev then
                    dst = -dst
                end
                if dst < bdst then
                    cand, bdst = i, dst
                end
            end
        end
    end
    if cand == 0 then
        if mode == "ai" then
            return getnextstep(from, to, rev, "smart")
        elseif mode == "smart" then
            return getnextstep(from, to, rev, "dist")
        end
    end

    return cand
end

function ai_queen(m)
    if not sight(m, 😐.pos) then
        m.task, m.active = ai_wait, false
        addfloat("?", m.pos, 10)
    else
        if m.charge == 0 then
            local cand = getnextstep(m.pos, 😐.pos)
            if cand > 0 then
                mobhop(addmob(2, m.pos), m.pos + dirpos[cand])
                m.charge = 2
                return true
            end
        else
            m.charge -= 1
            local cand = getnextstep(m.pos, 😐.pos, true)
            if cand > 0 then
                mobwalk(m, cand)
                m.trig = true
                return true
            end
        end
    end
    return false
end

function spawnmobs(ind, pack)
    local pospot, maxi = {}, #mob_plan[floor]

    for r in all(rooms) do
        if not r.nospawn then
            for p in all(r.tiles) do
                if isw(p, "smartmob") then
                    if r.avoid != true or (pack and noneighs(p)) then
                        add(pospot, p)
                    end
                end
            end
        end
    end

    while #pospot > 0 and ind <= maxi do
        local p = getrnd(pospot)
        addmob(mob_plan[floor][ind], p)
        del(pospot, p)
        ind += 1
    end
    if ind <= maxi and pack != true then
        spawnmobs(ind, true)
    end
    if dogate then
        for mb in all(mobs) do
            if mb.ai and not mb.we then
                mb.haskey = true
                return
            end
        end
    end
end

function droppick_rnd(pos)
    droppick(ceil(rnd(10)) + 1, pos)
end

function droppick(pick, pos)
    local dpos = dropspot(pos)
    local pck = addpick(pick, pos)
    mobhop(pck, dpos)
    pck.hoph = 12
end

function dropspot(pos)
    local best, bstd = 999, -1
    for p = 20, 305 do
        local d = dist(pos, p) + rnd()
        if d < best and isw(p, "smartmob") and not getpick(p) then
            best, bstd = d, p
        end
    end
    return bstd
end
-->8
--gen
function genfloor(f)
    buttbuff, steps, floor, turn, mobs, dmobs, picks, floats, wind, fog, voidmap = 0, 0, f, "player", { 😐 }, {}, {}, {}, { hpwind }, blankmap(0), blankmap(0)
    poke(0x3101, 66)
    if floor == 0 then
        copymap(0, 0)
        addmob(15, 153)
    elseif floor == winfloor then
        copymap(16, 0)
    else
        fog = blankmap(1)
        mapgen()
        unfog()
        showmsg("floor " .. floor)
    end
end

function mapgen()
    repeat
        flags, flaglib, tmap, roomap, rooms, voids = blankmap(0), {}, blankmap(2), blankmap(0), {}, {}
        dogate, epos = floor % 3 == 0
        if dogate then
            local mip = getrnd(explode2d("32,0|48,0|64,0|80,0|96,0|112,0|0,16|16,16|32,16|48,16|64,16|80,16|96,16|112,16"))
            copymap(mip[1], mip[2])
        end

        local fmax, rmax, mw, mh = 5, 4, 10, 10
        repeat
            local _w = 3 + flr(rnd(mw - 2))
            if placeroom({ w = _w, h = 3 + flr(rnd(mid(35 / _w, 3, mh) - 2)) }) then
                if #rooms == 1 then
                    mw /= 2
                    mh /= 2
                end
                rmax -= 1
            else
                fmax -= 1
            end
        until fmax <= 0 or rmax <= 0

        repeat
            local cand = {}
            for p = 20, 305 do
                if cancarve(p, false) and not nexttoroom(p) then
                    add(cand, p)
                end
            end

            if #cand > 0 then
                digworm(getrnd(cand))
            end
        until #cand <= 1

        local curf = 1
        for p = 20, 305 do
            if isw(p) and flags[p] == 0 then
                growflag(p, curf)
                add(flaglib, curf)
                curf += 1
            end
        end

        repeat
            local drs = {}
            for p = 20, 305 do
                if tmap[p] > -1 and tmap[p] != 86 and not isw(p) then
                    local found = sigarray(getsig(p), dor_sig, dor_msk)
                    local dif = found == 1 and 18 or 1
                    local _f1, _f2 = flags[p - dif], flags[p + dif]
                    if found > 0 and _f1 != _f2 then
                        add(drs, { pos = p, f1 = _f1, f2 = _f2 })
                    end
                end
            end
            if #drs > 0 then
                local d = getrnd(drs)
                tmap[d.pos] = 1
                growflag(d.pos, d.f1)
                del(flaglib, d.f2)
            end
        until #drs == 0
        --genrooms

        --mazeworm

        --placeflags

        --carvedoors
    until #flaglib == 1

    --carvescuts
    local x1, y1, x2, y2, cut, found, drs = 1, 1, 1, 1, 0
    repeat
        local drs = {}
        for p = 20, 305 do
            if tmap[p] > -1 and tmap[p] != 86 and not isw(p) then
                local found = sigarray(getsig(p), dor_sig, dor_msk)
                local dif = found == 1 and 18 or 1
                local p1, p2 = p - dif, p + dif
                if found > 0 then
                    calcdist(p1)
                    if distmap[p2] > 20 then
                        add(drs, p)
                    end
                end
            end
        end
        if #drs > 0 then
            tmap[getrnd(drs)] = 1
            cut += 1
        end
    until #drs == 0 or cut >= 3

    --startend
    local high = 0
    if not dogate then
        repeat
            epos = flr(rnd(286)) + 20
        until isw(epos)
    end
    calcdist(epos)
    for p = 20, 305 do
        if isw(p) and distmap[p] > high then
            spawnpos, high = p, distmap[p]
        end
    end
    calcdist(spawnpos)
    if not dogate then
        high = 0
        for p = 20, 305 do
            if distmap[p] > high and roomap[p] > 0 and freestanding(p) > 0 then
                epos, high = p, distmap[p]
            end
        end
    end
    high = 9999
    for p = 20, 305 do
        local tmp = distmap[p]
        if tmp >= 0 then
            local score = starscore(p)
            tmp = tmp - score
            if tmp < high and score >= 0 then
                spawnpos, high = p, tmp
            end
        end
    end
    if roomap[spawnpos] > 0 then
        rooms[roomap[spawnpos]].nospawn = true
    end
    tmap[spawnpos], 😐.pos, tmap[epos] = 15, spawnpos, 14

    --fillends
    repeat
        local filled = false
        for p = 20, 305 do
            if cancarve(p, true) and tmap[p] != 14 and tmap[p] != 15 then
                filled, tmap[p] = true, 2
            end
        end
    until not filled

    --prettywalls
    for p = 20, 305 do
        local tle, blw = tmap[p], not isw(p - 18)
        if tle == 86 then
            tle = 2
        end
        if tle == 2 then
            local ntle = sigarray(getsig(p), wall_sig, wall_msk)
            tle = ntle == 0 and 3 or 15 + ntle
            if fget(tle, 6) and rnd(8) < 1 then
                tle += 160
                if tle > 209 then
                    tle -= 33
                end
            end
        elseif tle == 1 and blw then
            tle = fget(tmap[p - 18], 3) and 180 or 4
        end
        tmap[p] = tle
    end

    if dogate then
        tmap[gpos] = 63
        if floor < 9 then
            addpick(0, ♥pos)
        end
    end

    --voids
    for p = 20, 305 do
        if tmap[p] == 3 then
            local cand, thisv, num, candnew = { p }, { tiles = { p }, walls = {} }, #voids + 1
            add(voids, thisv)
            voidmap[p], tmap[p] = num, 11
            repeat
                candnew = {}
                for c in all(cand) do
                    for d = 1, 4 do
                        local dpos = c + dirpos[d]
                        if tmap[dpos] == 3 and voidmap[dpos] == 0 then
                            voidmap[dpos], tmap[dpos] = num, getrnd(voitle)
                            add(thisv.tiles, dpos)
                            add(candnew, dpos)
                        elseif fget(tmap[dpos], 6) then
                            add(thisv.walls, dpos)
                        end
                    end
                end
                cand = candnew
            until #cand == 0
            thisv.exit = getrnd(thisv.walls)
            if thisv.exit and tmap[thisv.exit] > 176 then
                tmap[thisv.exit] -= 160
            end
        end
    end

    --chests
    local r, cand, telinlvl, p, pchest = getrnd(rooms), {}, floor >= 5 and rnd(5) < 1
    repeat
        pchest = getrnd(r.inside)
    until tmap[pchest] == 1
    for v in all(voids) do
        add(cand, getrnd(v.tiles))
    end
    if telinlvl then
        add(cand, pchest)
    end
    if #cand > 2 then
        for i = 1, 2 do
            p = getrnd(cand)
            del(cand, p)
            tmap[p] = 7
        end
    end
    if not telinlvl then
        add(cand, pchest)
    end
    for c in all(cand) do
        local chst = addmob(15, c)
        if floor >= 5 and rnd(5) < 1 then
            chst.♥ = true
        end
    end

    --deco
    tarr_vase, tarr_dirt, tarr_plant, funcs, func, rpot = explodeval("0,0,0,0,0,0,13,14"), explodeval("1,74,75,76"), explodeval("12,13,71"), explode("vase,dirt,carpet,torch,plant"), "vase", {}
    for r in all(rooms) do
        add(rpot, r)
    end
    repeat
        local r = getrnd(rpot)
        del(rpot, r)
        for pos in all(r.tiles) do
            local tle = tmap[pos]

            if func == "vase" then
                local mob = getrnd(tarr_vase)
                if mob > 0
                        and isw(pos, "smartmob")
                        and freestanding(pos) > 0
                        and find(r.edges, pos) then
                    addmob(mob, pos)
                end
            elseif func == "dirt" then
                if tle == 1 then
                    tle = getrnd(tarr_dirt)
                end
            elseif func == "carpet" then
                if tle == 1 then
                    if find(r.inside, pos) then
                        tle = 8
                    end
                end
            elseif func == "plant" then
                if tle == 1 then
                    tle = getrnd(tarr_plant)
                elseif tle == 4 then
                    tle = 6
                end
                if isw(pos, "smartmob") and not r.nospawn and rnd(4) < 1 and noneighs(pos) and freestanding(pos) > 0 then
                    local mb = getrnd({ 9, 9, 10 })
                    addmob(mb, pos)
                    if mb == 9 then
                        r.avoid = true
                    end
                end
            end

            if func == "torch" or func == "carpet" then
                if tle == 1 and find(r.edges, pos) then
                    local x, y = postoxy(pos)
                    if rnd(3) > 1 and y % 2 == 1 and freestanding(pos) > 0 then
                        if x == r.x then
                            tle = 64
                        elseif x == r.x + r.w - 1 then
                            tle = 66
                        end
                    end
                end
            end

            tmap[pos] = tle
        end

        if func != "plant" and r.nospawn != true then
            for p in all(r.tiles) do
                if tmap[p] == 1 and not getmob(p) and freestanding(p) > 0 then
                    if rnd(10) < 1 then
                        tmap[p] = 81
                    end
                end
            end
        end
        func = getrnd(funcs)
    until #rpot == 0

    spawnmobs(1)
end

function placeroom(r)
    local cand, c = {}
    for _x = 1, 17 - r.w do
        for _y = 1, 17 - r.h do
            if doesroomfit(r, _x, _y) then
                add(cand, { x = _x, y = _y })
            end
        end
    end

    if #cand == 0 then return false end

    c = getrnd(cand)
    r.x, r.y, r.pos, r.tiles, r.edges, r.inside = c.x, c.y, xytopos(c.x, c.y), {}, {}, {}
    add(rooms, r)
    for _x = 0, r.w - 1 do
        for _y = 0, r.h - 1 do
            local pos = xytopos(_x + r.x, _y + r.y)
            tmap[pos], roomap[pos] = 1, #rooms
            add(r.tiles, pos)
            if _x == 0 or _y == 0 or _x == r.w - 1 or _y == r.h - 1 then
                add(r.edges, pos)
            else
                add(r.inside, pos)
            end
        end
    end
    return true
end

function doesroomfit(r, x, y)
    for _x = -1, r.w do
        for _y = -1, r.h do
            if isw(xytopos(_x + x, _y + y)) then
                return false
            end
        end
    end
    return true
end

function digworm(pos)
    local dr, stp = 1 + flr(rnd(4)), 0
    repeat
        tmap[pos] = 1
        if not cancarve(pos + dirpos[dr], false) or (rnd() < 0.5 and stp > 2) then
            stp = 0
            local cand = {}
            for i = 1, 4 do
                if cancarve(pos + dirpos[i], false) then
                    add(cand, i)
                end
            end
            dr = #cand == 0 and 8 or getrnd(cand)
        end
        pos += dirpos[dr]
        stp += 1
    until dr == 8
end

function cancarve(pos, walk)
    if tmap[pos] == -1 or tmap[pos] == 86 then return false end
    local walk = walk == nil and isw(pos) or walk

    if isw(pos) == walk then
        return sigarray(getsig(pos), crv_sig, crv_msk) != 0
    end
    return false
end

function getsig(pos)
    local sig, digit = 0
    for i = 1, 8 do
        digit = isw(pos + dirpos[i]) and 0 or 1
        sig = shl(sig, 1) + digit
    end
    return sig
end

function bcomp(sig, match, mask)
    local mask = mask and mask or 0
    return bor(sig, mask) == bor(match, mask)
end

function sigarray(sig, arr, marr)
    for i = 1, #arr do
        if bcomp(sig, arr[i], marr[i]) then
            return i
        end
    end
    return 0
end

function nexttoroom(pos, dirs)
    local dirs = dirs or 4
    for i = 1, dirs do
        if roomap[pos + dirpos[i]] > 0 then
            return true
        end
    end
    return false
end

function growflag(pos, flg)
    local cand, candnew = { pos }
    flags[pos] = flg
    repeat
        candnew = {}
        for c in all(cand) do
            for d = 1, 4 do
                local dpos = c + dirpos[d]
                if isw(dpos) and flags[dpos] != flg then
                    flags[dpos] = flg
                    add(candnew, dpos)
                end
            end
        end
        cand = candnew
    until #cand == 0
end

function starscore(pos)
    if tmap[pos] > -1 then
        local scr = freestanding(pos)
        if roomap[pos] == 0 then
            if nexttoroom(pos, 8) then return -1 end
            if scr > 0 then
                return 5
            else
                if (cancarve(pos)) return 0
            end
        else
            if scr > 0 then
                return scr <= 8 and 3 or 0
            end
        end
    end
    return -1
end

function freestanding(pos)
    if tmap[pos] == -1 then return false end
    return sigarray(getsig(pos), free_sig, free_msk)
end

__gfx__
00000000000000006660666000000000666066606660666066606660a0aaa0a0505050500000000000000000000000000000000000000000a000000055555550
000000000000000000000000000000000000000000000000000000000a000a00000000000555555000000000000000000500005005050050a0aa000000000000
00700700000000006066606000000000606660606066606060666060a0a0a0a0505050500500005000505500000000000500005005000000a0aa0aa055000000
00077000000000000000000000000000000000000000000000000000a00000a000000000050000500000550000505000000500000000500000aa0aa055055000
00077000000000006660666000000000000000000000000000050500a0a0a0a0505050500555555005500000000000000005050000005050a0000aa055055050
007007000005000000000000000000000005000000550050050505000a000a00000000000000000005505000005050000505050005000050a0aa000055055050
00000000000000006066606000000000000000000555500005000000a0aaa0a0505050500555555000000000000000000500000005050050a0aa0aa055055050
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000006666660666666000666666006666600666666006660666066666660000066606660000066666660000066600000666066600000
00000000000000000000000066666660666666606666666066666660666666606660666066666660000066606660000066666660000066600000666066600000
00000000000000000000000066666660666666606666666066666660666666606660066066666660000006606600000066666660000066600000066066600000
00000000000000000000000066600000000066606660000066606660000066606660000000000000000000000000000000000000000066600000000066600000
00000660666666606600000066600000000066606660666066606660666066606660066066000660660006606600066000000660660066606666666066600660
00006660666666606660000066600000000066606660666066606660666066606660666066606660666066606660666000006660666066606666666066606660
00006660666666606660000066600000000066606660666066606660666066606660666066606660666066606660666000006660666066606666666066606660
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00006660066666006660000066600000000066600666666066606660666666006660666066606660666066606660666066606660666000006660666066666660
00006660666666606660000066600000000066606666666066606660666666606660666066606660666066606660666066606660666000006660666066666660
00006660666666606660000066600000000066606666666066000660666666606600066066006660660006606600066066600660660000006600666066666660
00006660666066606660000066600000000066606660000000000000000066600000000000006660000000000000000066600000000000000000666000000000
00006660666666606660000066666660666666606666666066000660666666606666666066006660000006606600000066600000666666600000666066000000
00006660666666606660000066666660666666606666666066606660666666606666666066606660000066606660000066600000666666600000666066600000
00006660066666006660000006666660666666000666666066606660666666006666666066606660000066606660000066600000666666600000666066600000
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00006660666666606660000066666660666066606660666066606660666066600000666066600000000066600000000066606660666000005000000000aaa000
000066606666666066600000666666606660666066606660666066606660666000006660666000000000666000000000666066606660000050550000a00000a0
00000660666666606600000066666660666066606660666066606660666066600000066066000000000006600000000066000660660000005055055000aaa000
000000000000000000000000000000006660666066600000666066600000666000000000000000000000000000000000000000000000000000550550a0a0a0a0
000000000000000000000000666666606660666066666660666666606666666066000000000006600000066066000660000000006600000050000550a0a0a0a0
000000000000000000000000666666606660666066666660666666606666666066600000000066600000666066606660000000006660000050550000a0aaa0a0
000000000000000000000000666666606660666006666660066666006666660066600000000066600000666066606660000000006660000050550550a0aaa0a0
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
06000000000000000000060000000000000000000000000000000000000000005050505050050000000000000000000000000000000000000000000000000000
600000000600000000000060000006000000000000a0a00000000000000005500000000005050050005000000000005000500000000000000000000000000000
6600000066000000000006600000066000000000000aa00000000000000055505055505000550500000005000050055000000500000000000000000000000000
0000000000000000000000000000000000a0a00000aaa00000000000050055000050500000000000000000000000000005000000000000000000000000000000
660000006600000000000660000006600a0aaa0000aaa00000a0a000005000005055505055000005000000000000000000055000000000000000000000000000
0005000000050000000500000005000000aaa000000a00000a0aaa0000550050000000000050550000050000005500500050050000aaaaa00000000000000000
60000000600000000000006000000060000a00000000000000aaa0005055050050505050050050500000000005555000005550000aaaaaaaa000000000aaaa00
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000aa0aaaaaaaa00000aaaaaaa0
00000000000000000000000000000000bbbbbbbbbbbbbbbbddd0ddd0bbbbbbbbbbbbbbbbbbb7bbbbbb777bbbbb7bbbbbbbbbbbbba00aaaaaaaaaaaaaaaaaaaaa
00707000070707000000000000000000bbbbbbbbbb000bbb00000000bb000bbbbbbbbbbbbbb7bbbbb77777bbb7b7bbbbbb7bbbbbaaaaaaaaaaaaaaaaaaaaaaaa
70777070007770000000000050505050bbb77bbbb00700bbd0ddd0d0b007000000000000bb777bbb7777777bbb7bb7bbbbbbbbbbaaaaaaaaaaaaaaaaaaaaaa0a
07777700777777700006000000000000bb7077bbb07770bb00000000b0777070707070707777777b7777777bbbbb7b7bbbbbb7bbaaaaaaaaaaaaaaaaaaaaa0aa
77707770077077006600066000000000bb7777bbb00700bbddd0ddd0b007000000000000bb777bbb7777777bb7bbb7bbbbbbbbbb0aaaaaaaaaaaaaaaaaaa0aa0
00000000000000006666666000000000bbb77bbbbb000bbb00000000bb000bbbbbbbbbbbbbb7bbbbb77777bb7b7bbbbbb7bbbbbb00aaaaaaaaaaaaaaa0a0a0a0
77777770777777706666666000000000bbbbbbbbbb070bbbd0ddd0d0bbbbbbbbbbbbbbbbbbb7bbbbbb777bbbb7bbbbbbbbbbbbbba00aaa0a0a0a0a0a0a0a0a0a
00000000000000000000000000000000bbbbbbbbbb000bbb00000000bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbba0000aaaa0a0a0a0a0aaa00a
0000000000006660000000006660000000000000bb070bbb000000000000000000000000bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbaa000000aaaaaaaaaaa000aa
0000000000006660000000006660000000000000bb000bbb000000000000000000000000bbbbbbbbbbb7bbbbbbbbbbbbbbbbbbbbaa000aa000000000000000aa
0000000000000660000050006600000000500000bb070bbb000000000aaa000000000000bbb7bbbbbbb7bbbbbb77bbbbbb7bbbbb0aa0000aaaaaaaaaa0000aa0
0000000000060660000000006606000000000000bb000bbb0aaa00000a0aaaa000000000bb777bbbb77077bbbb777bbbbbbbbbbb0a0aa00000000000000aa0a0
0000000000000660000050006600000000500000bb070bbb0a0aaaa00aaa0a000aaa0000bbb7bbbbbbb7bbbbbbb77bbbbbbb7bbb00a00aa0000000000aa00a00
0a000a0000006660000000006660000000000000bb000bbb0aaa0a00000000000a0aaaa0bbbbbbbbbbb7bbbbbbbbbbbbbbbbbbbb000aa00aaaaaaaaaa00aa000
00aaa00000006660000050006660000000500000bb070bbb00000000000000000aaa0a00bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb00000aa0000000000aa00000
0000000000000000000000000000000000000000bb000bbb000000000000000000000000bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb0000000aaaaaaaaaa0000000
00000000000000006666666000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000088888888
00000000000000006666666000000000000a0000000a000000000a000000a000000a000000aaa00000aaa000000a000000aa0a000a0a0a00aaa0000088888888
0000000000000000660006600000000000aaa0000000a000000a0a0000000a00000aa00000aaa0000000a00000a0a0000a00aa0000aaa000a0aaaa0088888888
000000000000000000060000000000000a0a0a000aaaaa000aaa0a000a0a0aa0000a0000000a0000000a000000a0a0000a0aaa000a0a0a00aaa0a00088888888
00000000000000000000000000000000000a0000000000000000000000000000000a0000000a0000000a0000000a000000000000000a00000000000088888888
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000088888888
a00000a0000000000000000050505050000000000000000000000000000000000000000000000000000000000000000000000000000000000000000088888888
0aaaaa00aaaaaaa00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000088888888
0000000000000000000000000000000000aaa000000000000000000000a000a0000000000aa00aa000000000000000000aaa0000000aa0000000000000000000
0000000000aaa0000000000000aaa0000aaaaaa0000000000aa00aa000a000a00aa00aa0a000a0000aaaa00000000000aa00a00000aaaa00000aa00000000000
00aaa0000aaa0a00000000000aaaaaa00aaaa0a000aaa000a000a0000a000a00a000a000a000a000aa000a000aaaaaa0a000000000a0a00000aaaa00000aa000
0aaa0a000aaa0a000aaaaaa00aaaa0a0aaaaaaa00aaaaaa00aaa0a000aaa0a000aaa0a000aaa0a00aaa00000aaa00000aaa00000a0aaaaa000a0a00000aaaa00
aaaaa0a00aaaaa00aaaaa00aaaaaaaa0aaaaa0000aaaa0a0a0aaa0a0aaaaa0a0aaa0a0a0a0a0a0a00aaaa0000aaaa0000aaaa000aaaaa0a0a0aaaaa000a0a000
aaaaaaa00aaaaa00aaaaaaaaaaaaa000aaaaaaa0aaaaaaa0aaa0a0a0a0a0a0a0a0aaa0a0aa0aa0a0a0aa0aa0a0aa0aa0a0aa0aa00aaa0000aaaaa0a0a0aaaaa0
0aaaaa0000aaa0000aaaaaa0aaaaaaa0aaa0a0a0aaaaa000a0aaaa00aa0aaa00aaa0aa00aaaaaa00a0a0aa00a0a0aa00a0a0aa00000000000aaa0000aaaaa0a0
000000000000000000000000aaa0a0a0aaaa0000aaaaaaa00000000000000000000000000000000000000000000000000000000000000000000000000aaa0000
000aaa00000aaa000000000000000000000000000a0000000000000000aa0000000aaa000000000000000aa0000000aa0000a0000000000000d00d0000000000
00aaaa0000aaaa0000aaaa0000aaaa000a0aaa00aaaaaa00000aaa000aa00a0000aaaaa000aaa00000aaa0000aaaaa00000a000000e00e00000dd00000d00d00
0aa0a0a00aaaaaa00aaaaa000aaaaa00aaa0a0a0aa0aa0a00aa0a0a00aa0aa0000aa00000aaaaa000a0aaa00a0aaaaa00aa0aa00000ee00000d00d00000dd000
0aaaaaa00aaaaaa00aaaaaa00aa0a0a0aa0aaaa0a0a0aaa0aaaaaaa000aaa000000aaa0000aaaa000aaaaa00aaaaaaa0a0aaa0a00ee0ee000dd0d0000dd0dd00
aaaaaaa0aaaaaaa0aaaaaaa0aaaaaaa0a0a0aa00a0aa0a00aa0aaa00aa0a0000aa0a0000aa0a000000aaa0000aaaaa00a0aaa0a00ee000000dd000000dd00000
aa0a00a0aa0a00a0aa0a00a0aa0a00a0a0aa0aa000aaa0a0a0a0aaa00aa0aa000aa0aa000aa0aa00a00000a0a00000a0aa0a0aa000eee00000ddd00000ddd000
a0aa0aa0a0aa0aa0a0aa0aa0a0aa0aa000aaa0a000aaaaa0a0aa0aa000aa000000aa000000aa00000aaa0a000aaa0a000aaaaa000eeeee000ddddd000ddddd00
000000000000000000000000000000000aaaaaa00aaaaaa00aaaa0a0000000000000000000000000000000000000000000000000000000000000000000000000
00aaa00000aaa000000000000a000a0000000000000a0a0000000000000a0a00000666606666666666066660bbbbbbbbbbbbbbbbbbbbbbbbb00000bb70000000
0a000a000a000a000aaaaaa0a00a00a0000a0a0000aaaa00000a0a0000aaaa00006666666666666666666660bbbbb000000bbbbbbbbbbbbbb07770bb77000000
0a000a000a000a000a0000a0a0aaa0a000aaaa00000a0aaa00aaaa00000a0aaa006660666666666666660666bbbb00700700bbbbbbbbbbbbb00700bb77700000
00aaa000a0aaa0a00a0aa0a0a00a00a0000a0aaa000aaaaa000a0aaa000aaaaa006660666666666666660666bbbb07700770bbbbbbbbbbbbbb000bbb77000000
0a00aa00aa00aaa00aaaaaa0a00a00a00a0aaaaa00a00000000aaaaa0a000000006606666666666666666066bbbb00700700bbbbbb000bbbbbbbbbbb70000000
0aaaaa000aaaaa000000000000000000aa0000000aa0aa000aa00000aa0aaa00000066666666666666666600bbbbb000000bbbbbb00700bbbbbbbbbb00000000
00aaa00000aaa0000aaaaaa0a0aaa0a0aa0aaa0a0aa0aa000aa0aa00aa0aaa0a000066666666666666666600bbbbbbbbbbbbbbbbb07770bbbbbbbbbb00000000
0000000000000000000000000000000000a00a00000aa00000a00a00000aa000000060606600000066060600bbbbbbbbbbbbbbbbb00000bbbbbbbbbb00000000
000000000000000060666660666066606600606055555550505050500666660000006606606666660660660000000000000a0aa000000000000a0aa000000000
0000500000000000666606606660660000000000000000000000000066666660000060606060660606060600000a0aa000a0aaaa000a0aa000a0aaaa00000000
000550500000000066666060660060006066006055555550050505006666666000066666606066060666666000a0aaaa00a0a0a000a0aaaa00a0a0a000000000
05005500000000000000000060600060000000000000000050505050600000600006666660666666066666600a00a0a00a00aaaa0a00a0a00a00aaaa00000000
55505550660666606600666066600660000000000000000055555550000000000006666666000000666666600aa0aaaaa0a00aa0a0a0aaaa0aa00aa000000000
00000000666606606660066006606660000500000005000005050500000000000000666666666666666660000a0a0aa0a0a00000a0a00aa00a0a000000000000
5555555066666060666600606660666000000000000000005555555000000000000000666666006666600000a00aa00aa000aa00a000a000a00aaa0a00000000
0000000000000000000000000000000000000000000000000000000000000000000000000066666600000000a0a00a00a00aa000a0a00a00a00aa00000000000
00006660000000006660000000000000000000000000000000000000000000000000066600000000006660000000000000000000000000000000000000000000
00006600000000000660000000000000000000000000000000000000000000000000060660000000066060000000000000000000000000000000000000000000
00006060000000006660000000000000000000000000000000000000000000000000000666000066666000000000000000000000000000000000000000000000
00006660000000006060000000000000000000000000000000000000000000000000000000066666000000000000000000000000000000000000000000000000
00000660000000006000000000000000000000000000000000000000000000000000000000660000000000000000000000000000000000000000000000000000
00006660000000006600000000000000000000000000000000000000000000000000006666600006666600000000000000000000000000000000000000000000
00006660000000006660000000000000000000000000000000000000000000000000006660000000066600000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000000000000000000000000000660000000066000000000000000000000000000000000000000000000
0000000066666060b0000000000000bbbbbbb000000000bb00000000000bbb0000000000000000000000000000000000000000000000000000000000000000bb
00000000666066600000000000000000bbb00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000b
000000006006666000777777777770000b000077777770000777777777700007777777700777777077777777000077777707777777007777707777777777700b
00000000000000000007777777777770000077777777777000777777777770007777770007777700077777700b0007777707777770007777707777777777700b
0000000000000000b00000777000777000077770000077770000777000777000007770000777000000077700bbb000777000077700007770000077700007700b
0000000000000000bb0000777000077700777000000000777000777000077700007770007770000b00077700bbbb00777000077700077700000077700000700b
0000000000000000bbbb007770000777007770000000007770007770000777000077700777000bbbb0077700bbbb00777000077700777000bb0077700000000b
0000000000000000bbbb00777000077700770000000000077700777000077700007770777000bbbbb0077700bbbb0077700007770777000bbb007770007000bb
0000000000000000bbbb0077700007700777007700077007770077700007700b00777777000bbbbbb0077700bbbb007770000777777000bbbb00777777700bbb
0000000000000000bbbb0077700777700777007700077007770077700077700b0077777000bbbbbbb0077700bbbb00777000077777000bbbbb00777777700bbb
0000000000000000bbbb007777777700077700770007700777007777777700bb00777777000bbbbbb0077700bbbb007770000777777000bbbb00777000700bbb
0000000000000000bbbb0077777770000777000700070007770077777777000b007777777000bbbbb0077700bbbb0077700007777777000bbb007770000000bb
0000000000000000bbbb007770000000077700000000000770007770077770000077707777000bbbb0077700bbb000777000077707777000bb0077700000000b
0000000000000000bbbb0077700000b000777000000000777000777000777700007770077770000bb0077700bb00007770000777007777000b0077700000700b
0000000000000000bbb000777000bbbb00777000000000777000777000077770007770007777000000077700000700777000077700077770000077700007700b
0000000000000000bb00007770000bbb00077770000077770000777000007777007770000777770000077700007700777000077700007777700077700007700b
aaaaaaaa00000000b0007777777000bbb0007777777777700007777700000777777777700077777700777777777707777770777777000777777777777777700b
aaaaaaaa00000000b0077777777700bbbb000077777770000077777770000077777777770000777707777777777707777770777777700007777777777777700b
a000000a00000000b0000000000000bbbbb000000000000000000000000b0000000000000000000000000000000000000000000000000000000000000000000b
a0a0aa0a00000000bb00000000000bbbbbbbb000000000bbb000000000bbb000000000000bb000000000000000000000000000000000bb0000000000000000bb
a000000a00000000bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb
00aa0a0000000000bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb
a000000a00000000bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb
aaaaaaaa00000000bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb
__label__
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000001777777700000000000016777777777610000000000007777777500000000000000000000000000000000000000
00000000000000000000000000000000000d77777777770000005777777777777777777777500000077777777776000000000000000000000000000000000000
00000000000000000000000000000000007777777777777006777777777777777777777777777600777777777777700000000000000000000000000000000000
00000000000000000000000000000000077777777777777777777777777777777777777777777777777777777777770000000000000000000000000000000000
00000000000000000000000000000000777777777777777777777777777777777777777777777777777777777777777000000000000000000000000000000000
00000000000000000000000000000000777777777777777777777777777777777777777777777777777777777777777000000000000000000000000000000000
00000000000000000000000000000005777777777707777777777777777777777777777777777777777707777777777600000000000000000000000000000000
0000000000000000000000000000000777777777d077777777777777777777777777777777777777777770577777777700000000000000000000000000000000
00000000000000000000000000000007777777700777777777777777777777777777777777777777777777007777777700000000000000000000000000000000
0000000000000000000000000000000777777700d777777777777777777777777777777777777777777777600777777700000000000000000000000000000000
00000000000000000000000000000007777770007777777777777777777777777777777777777777777777700077777700000000000000000000000000000000
00000000000000000000000000000007777700077777777777777777777777777777777777777777777777770007777700000000000000000000000000000000
00000000000000000000000000000007777d00d77777777777777777777777777777777777777777777777776005777700000000000000000000000000000000
00000000000000000000000000000007777000777777777777777777777777777777777777777777777777777000777700000000000000000000000000000000
00000000000000000000000000000007770007777777777777777777777777777777777777777777777777777700077700000000000000000000000000000000
0000000000000000000000000000f0077100077777777777777777777777777777777777777777777777777777000d7700000000000000000000000000000000
00000000000000000000000000000000000007777777777777777777777777777777777777777777777777777700000000000000000000000000000000000000
00000000000000000000000000000000000007777777777777777777777777777777777777777777777777777700000000000000000000000000000000000000
00000000000000000000000000000000000007777777777777777777777777777777777777777777777777777700000000000000000000000000000000000000
00000000000000000000000000000000000007777777777777777777777700000007777777777777777777777700000000000000000000000000000000000000
00000000000000000000000000000000000007777777777777777777770067777760077777777777777777777700000000000000000000000000000000000000
00000000000000000000000000000000000007777777777777777777777777777777777777777777777777777700000000000000f00000000f00000000000000
00000000000000000000000000000000000007777777777777777777700000000000007777777777777777777700000000000000000000000000000000000000
00000000000000000000000000000000000007777770000777777770007777777777700077777777000077777700000000000000000000000000000000000000
000000000000000000000000000000000000077777077000777777007777777777777770f7777770777007777700000000000000000000000000000000000000
00000000000000000000000000000000000007777707700077777007777777777777777700777770770007777700000000000000000000000000000000000000
00000000000000000000000000000000000007777700007077777007777777777777777700777770000707777700000000000000000000000000000000000000
00000000000000000000000000000000000007777700760077770077777077777770777770077770076007777700000000000000000000000000000000000000
00000000000000000000000000000000000007777770000777770077770077777770077770077777000077777700000000000000000000000000000000000000
00000000000000000000000000000000000007777777777777770077770077777770077770077777777777777700000000000000000000000000000000000000
0000000000000000000000000000000000d777777777777777770077770d7777777d077770077777777777777777600000000000000000000000000000000000
000000000000000000000000000000000d7777777777777777770077770677777776077770077777777777777777700000000000000000000000000000000000
00000000000000000000000000000000077777777777777777770077777077777770777770077777777777777777770000000000000000000000000000000000
00000000000000000000000000000000077777777777777777770077777777777777777770077777777777777777770000000000000000000000000000000000
0000000000f0000000000000000000000777777777777777777700677777777777777777600777777777777777777700000000000000000000000f0000000000
00000000000000000000000000000000077777777777777777777000777777777777777000777777777777777777770000000000000000000000000000000000
000000000000000000000000f00000000d7777777777777777777700000000000000000007777777777777777777700000000000000000000000000000000000
0000000000000000000000000000000000777777777777777777777700000000000000077777777777777777777770f000000000000000000000000000000000
00000000000000000000000000000000000777777777777777777777777777777777777777777777777777777777000000000000000000000000000000000000
000000000000000000000000000000000000d7777777777777777777777777777777777777777777777777777760000000000000000000000000000000000000
00000000000000000000000000000000000000177777777777777777777777777777777777777777777777775000000000000000000000000000000000000000
00000000000000000000000000000000000000000777777777777777777777777777777777777777777777000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000777777777777777700000007777777777777777100000000000000000000000000000000000000000000
000000000000000000000000000000000000000000000000d7777777777d7777777d777777777760000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000000001777777777777777777777500000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000000000005777777777777777d00000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000000000000007777777777700000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000007777600000000000000000000000000000006777700000000000000000000000000000000000000000000
00000000000000000000000000000000000000000077777750000000000000000000000000000017777770000000000000000000000000000000000000000000
00000000000000000000000000000000000000000777777770000000000000000000000000000077777777000000000000000000000000000000000000000000
00000000000000000000000000000000000000006777707770000000000000000000000000000077707777600000000000000000000000000000000000000000
00000000000000000000000000000000000000007777107777000000000000000000000000000777700777700000000000000000000000000000000000000000
00000000000000000000000000000000000000001776007777770000000000000000000000077777700677100000000000000000000000000000000000000000
00000000000000000000000000000000000000000000007777777750000000000000000017777777700000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000007777777777600000000000006777777777700000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000001770006777000000000000777777600077100000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000000000000000000000577777700000000000000000000000000000000000000000000000000000000
000000000000000000000000000000000000000000000000000000000000000677777d0000000000000000000000000000000000000000000000000000000000
000000000000000000000000000000000000000000000000000000000000d7777770000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000000000000007777776000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000000000000777777500000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000005777770001777777700000000000577100077777d00000000000000000000000000000000000000000000
00000000000000000000000000000000000000000077777777777777770000000000067777777777777770000000000000000000000000000000000000000000
00000000000000000000000000000000000000000777777777777777000000000000000777777777777777000000000000000000000000000000000000000000
0000000000000000000000000000000000000000077777777777760000000000000000000d777777777777000000000000000000000000000000000000000000
00000000000000000000000000000000000000000777777777770000000000000000000000077777777777000000000000000000000000000000000000000000
00000000000000000000000000000000000000000777777777770000000000000000000000077777777777000000000000000000000000000000000000000000
00000000000000000000000000000000000000000077777777770000000000000000000000077777777770000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000007777770000000000000000000000077777700000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000007777770000000000000000000000077777600000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000006777710000000000000000000000017777000000000000000000000000000000000000000000000000
000000000000000000000000000000f00000000000000000000000000f0000f000000000000000000000000000000000000000000000000000000000000f000f
000000000000000000000000000000000000000000000000000000000000000000000000000000000f0000000000000000000000000000000000000000000000
000000000000000000000000000000000000000000f000f00000000000000000000000000000000000000000000000000f000000000000000000000000000000
000000000000000000000000000000000000f00000000000000000000000000000000000000000000000000000000000000000000000000000000000f0000000
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000f000000f000000000000f000000000000000
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
000000000000000000000000000000000000000000000000000000000000000000f00000000000f00000000f0000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000f000000000000000000000000000000000000000000000000000000000000000000f0000000000000
0000000000000000000000000000000000f00000000000f00000000000000000000000000000000000ff000000000000000000f0000000000000000000000000
00000000007777777777700000000077777770000777777777700007777777700777777077777777000077777707777777007777707777777777700000000000
0000000000077777777777700000777777777770007777777777700077777700077777000777777000000777770777777000777770777777777770000f000000
00000000000000777000777000f77770000077770000777000777000007770000777000000077700000000777000077700007770000077700007700000000000
00000000000000777000077700777000000000777000777000077700007770007770000000077700000000777000077700077700000077700000700000000000
0000000000000077700007770077700000000077700077700007770000777f077700000000077700000000777000077700777000000077700000000000000000
0000000000000077700007770077000000000007770f777000f77700f07770777000000000f77700000000777000077707770000000077700070000000000000
000000000000007770000770f7770077000770077700777000077000007777770000f000f0077700000000777f00077777700000000077777770000000000000
000000000000007770077770077700770007700777007770007770000077777000000000000777000f0000777000077777000000000077777770000000000000
000000000000007777777700077700770007700777007777777700000077777700000000f0077700000000777000077777700000000077700070000000000000
0000000000000077777770000777000700f70f077700777777770000007777777000000000077700000000777000077777770000000077700000000000000000
00000000000000777000000007770000000000077000777007777000007770777700000000077700000000777f00077707777000000f77700000000000000000
0000000000000f7770000000007770000000007770007770007777000077700777700000f00777f0000000777000077700777700000077700000700000000000
0f00000000000f7770000000f077700000000f777000777000077770007770007777000000077700000700777000077700077770000077700007700000000000
00000000000f007770000000000777700000777700007770000077770077700007777700000777000077007770f00777000077777000777000f7700000000000
00000000000077777770000000007777777777700007777700000777777777700077777700777777777707777770777777000777777777777777700000000000
00000000000777777777000000000077777770000077777770000077777777770000777707777777777707777770777777700007777777777777700000000000
0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000f000f0f000000000000000000000000000000
00000f0000000000000000000000000000000000000000000000000000000000000000000000000000000000f000000000000000000000000000f00000000000
00000000000000000000000007070707077700770777000000770077077707770077000007770077000007070077077700770777000000000000000000000000
00000000000000000000000f07070707070707000070000007000707077707000700000000700707000007070707070707000070000000000000000000000000
00000000000f00000000000007070707077007770f700000f70f0707070707700777000000700707000007070707077007770070000000000000000000000000
00000000000000000000000007770707070700070070f000070007070707070000070000007007070000f7770707070700070070000000000000000000000000
000000000000000000000f000777007707070770007000000077077007070777077000000070077000000777f7700707077000700000000000000000000000f0
000000000000000000000000000000000000000000000000000000000f000000000000000000000000f000000000000000000000000f00000000000000000000
000000000000000000000000000000000000000000000000000000000000000000000000f0000000000000000000000000000000000000000000000000000000
00000f0000000000000000f00000000000000000000000f0000000000000000000000000000000000000000f0000f00000000000000000000000000000000000
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
0000000000000f00000000000000000000000000000000000000000000000000000000000000000000000000000000000000f000000000000000000000000000
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000000000000f00000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000f0000000000000000000000000000000000000f0000000000000000000000000f000000000000f000000
00000000000000000000000000000000000000000000000000000000000000000000000f00000000000000000000000000000000000000000000000000000000

__gff__
0000050520202002000000000000020085c58585858585858585858585858585c585c58585858585858585858585858585c585c5c5858585858585858585000700000000000000040000000000010101020287000000050000000000000101010087008700000000000000000001020100008700000000000000000000000000
00000000000000000000000000000000000000000000000000000303000000000303030000000000000000000000000000cdcdcd202000c50000000000000000cd00cd0000000000000000000000000000cd00000000000000000000000000000303030000000000000000000000000000000000000000000000030000000000
__map__
0303030303030303030303030303030303030303030303030303030303030303020202020202020202020201565601010202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020201010101010101010202020202020202020202020202020202020202
03030303030303030303030303030303030303030303030303030303030303030202020202020202020202013f01440e0202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202565656565656020202020202020202020202020202020202020202
0303030303030303030303030303030303030303101111111111120303030303020202020202020202020201565601010202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202560101010156020202020202020202020202020202020202020202
0303030303031011120303030303030303030303200202020202220303030303020202020202020202020202025656560202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202560e44080156020202020202020202020202020202020202020202
030303101111240e231111120303030303030303200202020202220303030303020202020202020202020202020202020202020202020201010102020202020202020202020202020202020202020202020202020202020202020202020202020202020202565656563f56020202020202020202020202020202020202020202
030303200404040104050422030303030303030320040404040422030303030302020202020202020202020202020202020202020202565656565602020202025656565602020202020202020202020202020202020202020202020201565656020202020201010101b501020202020202020202020202020202020202020202
030303204a01080808014b22030303030303030320404d4e4f432203030303030202020202020202020202020202020202020202020256010e01560202020202010e0156020202020202020202020202020202020202020202020202015601010202020202020202020202020202020202020202020202020202020202020202
030303204001080808014222030303030303030320015d5e5f01220303030303020202020202020202020202020202020202020202025608440856020202020208440856020202020202020202020202020202020202020202020202013f080e0202020202020202020202020202020202020202020202020202020202020202
030303200c01080808010122030303030303030320416d6e6f422203030303030202020202020202020202020202020202020202020256b5b5b5560202020202b5b5b556020202020202020202020202020202020202020202020202015608440202020202020202020202020202020202020202020202020202020202020202
03030320010101010101012203030303030303032001010101012203030303030202020202020202020202020202020202020202020256563f56560202020202563f5656020202020202020202020202020202020202020202020202015601010202020202020202020202020202020202020202020202020202020202020202
030303204a01010101010d2203030303030303033014b5b5b513320303030303020202020202020202020202020202020202020202020101b50101020202020201b50101020202020202020202020202020202020202020202020202015656560202020202020202020202020202020202020202020202020202020202020202
03030330313114b51331313203030303030303030320010f0122030303030303020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020201010101020202020202
03030303030320b6220303030303030303030303033031313132030303030303020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020256563f56020202020202
03030303030330b73203030303030303030303030303030303030303030303030202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202560e4456020202020202
0303030303030303030303030303030303030303030303030303030303030303020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020256565656020202020202
0303030303030303030303030303030303030303030303030303030303030303020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020101010101010202020202
020202020256010e01560202020202020e015601020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202
0202020201560144015601020202020244015601020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202
02020202025656b5565602020202020208565601020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202
020202020202563f5602020202020202b5013f01020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202
02020202020202b5020202020202020256565601020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020102020202020202020202020202020202020202020202020202020202020202
0202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020156565602020202020202020202020202020202020202020202020202020201
02020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020201560e5602020202020202020202020202020202020202020202025656565601
0202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020156445602020202020202020202020202020202020202020201565601015601
02020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020201560856565601020202020202020202020202020202020202013f01080e5601
0202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020256560156b501013f0102020202020202020202020202020202020201560108445601
02020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020256565601020202020202020202020202020202020202020101010202020202020202020202020202020202020202560e0156565656560102020202020202020202020202020202020201565601015601
020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202010e560102020202020202020202020202020202020202563f56020202020202020202020202020202020202020256080102020202020202020202020202020202020202020202020202025656565601
0202020202020202020202020202020202020202020202020202020202020202020202020202020202020202010101014456560102020202020202020202020202020202020256560156560202020202020202020202020202020202020256440202020202020202020202020202020202020202020202020202020202020201
020202020202020202020202020202020202020202020202020202020202020202020202020202020202020256563f56085601010202020202020202020202020202020202565601b501565602020202020202020202020201010202020256080202020202020202020202020202020202020202020202020202020202020202
02020202020202020202020202020202020202020202020202020202020202020202020202020202020202025601b50108563f5602020202020202020202020202020202015601084408015601020202020202020202020201565656565656080202020202020202020202020202020202020202020202020202020202020202
020202020202020202020202020202020202020202020202020202020202020202020202020202020202020256080e44b501015602020202020202020202020202020202015656080e085656010202020202020202020202013f0101010101b50202020202020202020202020202020202020202020202020202020202020202
__sfx__
0002000031530315302d500315003b5303b5302e5000050031530315302e5002d50039530395302d5000050031530315303153031530315203152000500005000050000500005000050000500005000050000500
000100003101031010300102f0102d0202c0202a02028030270302503023050210501e0501d0501b05018050160501405012050120301103011010110100e0100b01007010000000000000000000000000000000
00010000240102e0202b0202602021010210101a01000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
0001000024010337203372033720277103a7103a71000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00030000096201163005620056150160000600006001160011600116001160001620006200a6100a6050a6000a6000f6000f6000f6000f6000060000600026100261002615016000160005600056000160001600
00010000145201a520015000150001500015000150000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00010000211102114015140271300f6300f6101c610196001761016600156100f6000c61009600076000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
000100001b61006540065401963018630116100e6100c610096100861000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
000100001f5302b5302e5302e5303250032500395002751027510285102a510005000050000500275102951029510005000050000500005002451024510245102751029510005000050000500005000050000500
00010000132201322013240132401123012230102100f2100d2100d2100a2100a21009210092100a2100a2100a2100a2100a2100a2100a2100a2100a2100a2100a2100a2100a2100a2100a2100a2100020000200
0001000010030100301c0302e0302f0102f010300103205033050350503c0503c0500000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00010000350402f0402c0402a0402804026030250302302022020200201e0101d0101c0101b0101b0101a0101a010190101801018000170001700017000170001700017000160001600015000150001500016000
000100000d720137200d7100c400312003120000000000000000000000000000000000000000002a0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00010000210302703025040230301a0300d0102000022000230002700023000200001f000200001f0001b0001f00022000200002200023000270001d000200001f0001f0001f0001f00000000000000000000000
00030000236502c660206703c670366601c660256501b650146401a6400f6300d6301562008610066100561003610026100160001600006000060000600156002250022500225002250000000000000000000000
00100000152201a2201d23022240212401d2401523014230142201000001000011000800010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00020000206301d6301d6202620025200212001b6301b6301b6200000000000000001c6201b6201b6200000000000196001d6101b610196100000000000000000000000000000000000000000000000000000000
0002000033640386303c6203e6103e6103e6153e61500000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
0002000022510265101c640266001a6202560022600226000e61026600216000e6000060000600006000060000600006000060000600006000060000600006000060000600006000060000600006000060000600
00020000036300660006620245102c5002b5103c51004600066200360002620026101e70026700307003570000700007000070000700007000070000700007000070000700007000070000700007000070000700
000200001c5301c5302d5003150025530255302e5000050031530315302e5002d50039530395302d5000050039530395303953039530395203952000500005003950039520395203952039510395103951039500
0001000024010337203372033720277103a7103a710000001724013240112300e2100c2100a210092100921009210092100921009210092100921009210092100921009210092100621003210012100000000000
001000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
001000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
001300001170015700197001a700117001670019700197001a7001a70025700257002570025700257002570025700197021970219702000000000000000000000000000000000000000000000000000000000000
00120000030000300003000130000700007000080000800008000170000b0000b0000a0000a0000a0000f00003000030000800008000080001100005000050000300003000030000300003000030000300000000
0116000021005210051d00515015150151a0151a0151d0151d015220152201521015210151d0151d01515015150151401014012140151401518000000000000000000100100c0100d01010010140101501014010
0116000000000000002000015015150151a0151a0151d0151d015220152201521015210151d0151a01526015260152501019015190151900518000000000000000000000000d0101101014010150101401019010
0116000000000000000000022015220151f0151f0151a0151a01522015220151f0151f01519010190121a0151a0151f0101f012130151300518000000000000000000000000f0101301016010150101601215010
01160000190051901519015220152201521015210151c0151c015220152201521015210151c0121c0151d0151d015200102001220015200051d0051a015220152901029012260102801628010280122801528005
01160000097140e720117300e730097250e7251173502735057240e725117350e735097450e7401174002740087400d740107200d720087350d7351072501725047240d725107250d725087350d7301074001740
01160000097240e720117300e730097450e745117350e735117240e725117350e735097450e740117400e740087400d740117200d720087350d735117250d725117240d725117250d725087350d730117400d740
011600000a7240e720137300e7300a7450e745137350e735137240e725137350e7350a7450e740137400e7400a7400f740137200f7200a7350f735137250f725137240f725137250f7250a7350f730137400f740
0116000010724097201073009730107450974510735097351072409725107350973510745097401074009740117400e740117200e720117350e735117250e725117240e725117250e725097350d730107400d740
011600000217502705021150200002135000000210402104021250000002105000000215500000000000211401175017050111500105011350010500105001050112500105001050010501135001000000000000
01160000215101d510195251a535215351d520195151a5152151221515215252252521525215150e51511515205141c510195251c535205351c520195151c5152051220515205252152520525205150d51510515
0116000000000215101d510195151a515215151d510195151a5152151221515215152251521515215150e51511515205141c510195151c515205151c510195151c5152051220515205152151520515205150d515
01160000150051d00515015150151a0251a0151d0151d015220252201521025210151d0251d0151502515015140201402214025140151400514004140050d000100140c0100d0201003014030150201401210015
011600000217502705021150200002135000000000000000021250000000000000000215500000000000211405175001050511500105051350010500105001050512500105001050010505135000000000000000
01160000215141d510195251a525215251d520195151a5152151221515215202252021525215150e52511515205141d5101852519525205251d520185151951520512205151c5201d52020525205151052511515
0116000000000215141d510195151a515215151d510195151a5152151221515215102251021515215150e51511515205141d5101851519515205151d510185151951520512205151c5101d510205152051510515
01160000000002000015015150151a0251a0151d0251d015220252201521015210151d0251d01526015260152502025012250152501518000000000000000000100000d02011030140401505014040190301d010
011600000717502005071150200007135000000000000000071250000000000000000715500000000000711403175001050311500105031350010500105001050312500105001050010503155000000000000000
01160000091750200509115020000913500000000000000009125000000000000000091550000000000091140a175001050a115001050a1250010504105001050a125001050910500105041350c1000912500100
01160000225121f5201a5251f515225251f5201a5151f515215122151222525215251f5251f5150e52513515225141f5101b5251f525225251f5201b5151f515215122151222525215251f5251f5150f52513515
01160000215141c510195251d515215251c520195151d5152151222510215201f51021512215150d52510515205141d5101a52516515205151d5201a5151651520522205151d515205251f5251d5151c52519515
0116000000000225121f5101a5151f515225151f5101a5151f515215122151222515215151f5151f5150e51513515225141f5101b5151f515225151f5101b5151f515215122151222515215151f5151f5150f515
0116000000000215141c510195151d515215151c510195151d5152151222510215101f51021510215150d51510515205141d5101a51516515205151d5101a5152051520510205151d515205151f5151d5151c515
01160000000000000022015220151f0251f0151a0151a01522025220151f0151f01519020190221a0251a0151f0201f0221f0151f01518000000000000000000000000f010130201603015030160321502013015
011600001902519015220252201521015210151c0251c015220252201521025210151c0221c0151d0251d01520020200222001520015110051a0151d015220152601226012280102601625010250122501025015
011600000217509035110150203502135090351101502104021250000002105000000212511035110150211401175080351001501035011350803510015001050112500105001050010501135100351001500000
0116000002175090351101502035021350903511015021040212500000021050000002155110351101502114051750c0351401505035051350c03514015001050512500105001050010505135140351401500000
01160000071750e0351601507035071350e0351601502104071250000002105000000715516035160150711403175160351301503035031351603513015001050312500105001050010503135160351601500000
0116000009175100351101509035091351003511015021040912500000021050000009155100350d015091140a17510035110150a0350a1351003511015001050a12500105001050010509135150350d01509020
0116000002215020451a7051a7050e70511705117050e7050e71511725117250e7250e53511535115450e12501215010451a6001a70001205012051a3001a2001071514725147251072510535155351554514515
0116000002215020451a7051a7050e70511705117050e7050e71511725117250e7250e53511535115450e12505215050451a6001a70001205012051a3001a2001171514725147251172511535195351954518515
0116000007215070451a7051a7050e70511705117050e705137151672516725137251353516535165451312503215030451a6001a70001205012051a3001a2001371516725167250d7250f535165351654513515
0116000009215090451a7051a7050e70511705117050e7050d715157251572510725115351653516545157250a2150a0451a6001a70001205012051a3001a2000e71510725117250e7250d5350e5351154510515
011200001e0201e0201e032210401a0401e0401f0301f0321f0301f0301e0201e0201f0201f020210302103022030220322902029020290222902228020280202602026020260222602200000000000000000000
011200001a7041a70415534155301a5321a5301c5401c5401c5451a540155401554516532165301a5301a5351f5401f54522544225402254222545215341f5301e5441e5401e5421e54500000000000000000000
01120000110250e000120351500015045150000e0550e00512045150051503515005130251500516035260051a0452100513045210051604526005100251f0050e0500e0520e0520e0500c000000000000000000
011300000d2200c2200b220154000000000000000000000029720287302672626745287402173029720217322673026732267350210526702267020e705021050000000000000000000000000000000000000000
0113000000000000000000000000000000000000000000000e1100d1200a1300e1350d135091000a120091300e1220e1200e1200e1000e1020e10200000000000000000000000000000000000000000000000000
0113000000000000000000000000000000000000000000000a14300000000000a060090600a000090000900002072020720207202005020020200500000000000000000000000000000000000000000000000000
__music__
03 22424344
01 22251a43
00 26291b47
00 2a301c4e
00 2b311d4f
00 32232443
00 33272847
00 342c2e4e
00 352d2f4f
00 221e2544
00 261f2944
00 2a203044
00 2b213144
00 22365c44
00 26375d44
00 2a385e44
02 35395f44
00 41424344
00 41424344
00 41424344
00 41424344
00 68696744
04 3d3e3f44
00 6d6e6f44
04 3a3b3c44

