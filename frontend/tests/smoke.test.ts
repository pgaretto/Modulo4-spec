describe('infraestructura de tests', () => {
  it('ejecuta Vitest con jsdom y simula <dialog>', () => {
    const dialog = document.createElement('dialog')
    dialog.showModal()
    expect(dialog).toHaveAttribute('open')
  })
})
