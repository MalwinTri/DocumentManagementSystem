import * as React from "react";
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
} from "@/components/ui/alert-dialog"; // shadcn/ui

export type ConfirmOptions = {
    title?: React.ReactNode;
    description?: React.ReactNode;
    confirmText?: string;
    cancelText?: string;
    destructive?: boolean; // roter Button
};

type ConfirmFn = (opts?: ConfirmOptions) => Promise<boolean>;

const ConfirmContext = React.createContext<ConfirmFn | null>(null);

export function useConfirm(): ConfirmFn {
    const ctx = React.useContext(ConfirmContext);
    if (!ctx) throw new Error("useConfirm must be used inside <ConfirmProvider />");
    return ctx;
}

export function ConfirmProvider({ children }: { children: React.ReactNode }) {
    const [open, setOpen] = React.useState(false);
    const [opts, setOpts] = React.useState<ConfirmOptions>({});
    const resolver = React.useRef<(v: boolean) => void>();

    const confirm = React.useCallback<ConfirmFn>((options) => {
        setOpts(options ?? {});
        setOpen(true);
        return new Promise<boolean>((resolve) => {
            resolver.current = resolve;
        });
    }, []);

    const close = (value: boolean) => {
        setOpen(false);
        resolver.current?.(value);
    };

    return (
        <ConfirmContext.Provider value={confirm}>
            {children}
            <AlertDialog open={open} onOpenChange={setOpen}>
                <AlertDialogContent className="rounded-2xl">
                    <AlertDialogHeader>
                        <AlertDialogTitle>{opts.title ?? "Are you sure?"}</AlertDialogTitle>
                        {opts.description && (
                            <AlertDialogDescription>{opts.description}</AlertDialogDescription>
                        )}
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                        <AlertDialogCancel className="rounded-xl" onClick={() => close(false)}>
                            {opts.cancelText ?? "Cancel"}
                        </AlertDialogCancel>
                        <AlertDialogAction
                            className={`rounded-xl ${opts.destructive ? "bg-red-600 hover:bg-red-700" : ""
                                }`}
                            onClick={() => close(true)}
                        >
                            {opts.confirmText ?? "Continue"}
                        </AlertDialogAction>
                    </AlertDialogFooter>
                </AlertDialogContent>
            </AlertDialog>
        </ConfirmContext.Provider>
    );
}