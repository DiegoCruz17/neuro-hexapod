

function [DEV1, DEV2] = DERIVATOR(DEV1, DEV2,U, dt)
    DEV1 = DEV1 + (dt / 30) * (-1 * DEV1 + 1 * U);
    DEV2 = DEV2 + (dt / 40) * (-1 * DEV2 + 1 * U - 1 * DEV1);
end